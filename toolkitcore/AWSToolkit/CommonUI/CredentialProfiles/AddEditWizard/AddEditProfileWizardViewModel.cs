using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime.CredentialManagement;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class AddEditProfileWizardViewModel : ViewModel, IAddEditProfileWizard
    {
        // IAM Identity Center for AWSSDK references
        // https://docs.aws.amazon.com/sdkref/latest/guide/understanding-sso.html
        // https://docs.aws.amazon.com/sdkref/latest/guide/feature-sso-credentials.html

        private const int _validateConnectionTimeoutMillis = 10000;

        private const int _saveTimeoutMillis = 10000;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(AddEditProfileWizardViewModel));

        private IAddEditProfileWizardHost _addEditProfileWizardHost => ServiceProvider.GetService<IAddEditProfileWizardHost>();

        private WizardStep _currentStep = WizardStep.Configuration;

        public WizardStep CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        private bool? _status;

        public bool? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _inProgress;

        public bool InProgress
        {
            get => _inProgress;
            set => SetProperty(ref _inProgress, value);
        }

        #region OpenCredentialFile

        public event EventHandler<CredentialsFileOpenedEventArgs> CredentialsFileOpened;

        protected virtual void OnCredentialsFileOpened(CredentialsFileOpenedEventArgs e = null)
        {
            CredentialsFileOpened?.Invoke(this, e ?? CredentialsFileOpenedEventArgs.Empty);
        }

        private ICommand _openCredentialsFileCommand;

        public ICommand OpenCredentialsFileCommand
        {
            get => _openCredentialsFileCommand;
            private set => SetProperty(ref _openCredentialsFileCommand, value);
        }

        private void OpenCredentialsFile(object parameter)
        {
            var shared = new SharedCredentialsFile();
            OpenFileInEditor(shared.ConfigFilePath);
            OpenFileInEditor(shared.FilePath);

            OnCredentialsFileOpened();
        }

        private void OpenFileInEditor(string path)
        {
            // Touch the credentials file or OpenInEditor will fail with an error if it doesn't exist
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                File.WriteAllText(path,
@"# AWS credentials file used by AWS CLI, SDKs, and tools.
# Created by AWS Toolkit for Visual Studio. https://aws.amazon.com/visualstudio/
#
# Each [section] in this file declares a named ""profile"", which can be selected
# in tools like AWS Toolkit to choose which credentials you want to use.
#
# See also:
#   https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html
#   https://docs.aws.amazon.com/cli/latest/userguide/cli-config-files.html");
            }

            ToolkitContext.ToolkitHost.OpenInEditor(path);
        }
        #endregion

        #region Save

        public async Task<ActionResults> SaveAsync(ProfileProperties profileProperties, CredentialFileType fileType, bool changeConnectionSettings = true)
        {
            Status = null;
            InProgress = true;

            var actionResults = await ValidateConnectionAsync(profileProperties);
            if (!actionResults.Success)
            {
                Status = false;
                InProgress = false;
                return actionResults;
            }

            try
            {
                var credId = fileType == CredentialFileType.Shared ?
                    new SharedCredentialIdentifier(profileProperties.Name) as ICredentialIdentifier :
                    new SDKCredentialIdentifier(profileProperties.Name);
                var region = ToolkitContext.RegionProvider.GetRegion(profileProperties.Region);

                using (var cancelSource = new CancellationTokenSource(_saveTimeoutMillis))
                {
                    await ToolkitContext.CredentialSettingsManager.CreateProfileAsync(credId, profileProperties, cancelSource.Token);
                    if (changeConnectionSettings)
                    {
                        await ToolkitContext.ConnectionManager.ChangeConnectionSettingsAsync(credId, region, cancelSource.Token);
                        _addEditProfileWizardHost.NotifyConnectionSettingsChanged(credId);
                    }
                }

                actionResults = new ActionResults().WithSuccess(true);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unable to save and/or set profile {profileProperties?.Name} in AWS Explorer.", ex);

                Status = false;
                actionResults = ActionResults.CreateFailed(ex);
            }
            finally
            {
                RecordAwsModifyCredentialsMetric(actionResults);
                InProgress = false;
            }

            return actionResults;
        }

        private int _connectionAttempts = 0;

        internal async Task<ActionResults> ValidateConnectionAsync(ProfileProperties profileProperties)
        {
            ++_connectionAttempts;

            using (var cancelSource = new CancellationTokenSource(_validateConnectionTimeoutMillis))
            {
                var actionResults = ActionResults.CreateFailed();

                try
                {
                    var isValid = ConnectionState.IsValid(await profileProperties.ValidateConnectionAsync(ToolkitContext, cancelSource.Token));
                    actionResults = new ActionResults().WithSuccess(isValid);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.Error($"Unable to validate credentials for {profileProperties?.Name}.", ex);
                    actionResults = ActionResults.CreateCancelled();
                }

                RecordAuthAddConnectionMetric(actionResults);

                return actionResults;
            }
        }
        #endregion

        #region OpenLogsCommand

        private ICommand _openLogsCommand;

        public ICommand OpenLogsCommand
        {
            get => _openLogsCommand;
            private set => SetProperty(ref _openLogsCommand, value);
        }
        #endregion

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<IAddEditProfileWizard>(this);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            OpenLogsCommand = new OpenToolkitLogsCommand(ToolkitContext);
            OpenCredentialsFileCommand = new RelayCommand(OpenCredentialsFile);
        }

        private T CreateMetricData<T>(ActionResults actionResults) where T : BaseTelemetryEvent, new()
        {
#if DEBUG
            if (_addEditProfileWizardHost.SaveMetricSource == null)
            {
                throw new InvalidOperationException($"{nameof(IAddEditProfileWizardHost.SaveMetricSource)} must be set by wizard host.");
            }
#endif
            var accountId = ToolkitContext.ConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet;

            return actionResults.CreateMetricData<T>(accountId, MetadataValue.NotApplicable);
        }

        private void RecordAwsModifyCredentialsMetric(ActionResults actionResults)
        {
            var saveMetricSource = _addEditProfileWizardHost.SaveMetricSource;

            var data = CreateMetricData<AwsModifyCredentials>(actionResults);
            data.Result = actionResults.AsTelemetryResult();
            data.CredentialModification = CredentialModification.Add;
            data.Source = saveMetricSource?.Location ?? MetadataValue.NotSet;
            data.ServiceType = saveMetricSource?.Service ?? MetadataValue.NotSet;

            ToolkitContext.TelemetryLogger.RecordAwsModifyCredentials(data);
        }

        private void RecordAuthAddConnectionMetric(ActionResults actionResults)
        {
            var saveMetricSource = _addEditProfileWizardHost.SaveMetricSource;

            var data = CreateMetricData<AuthAddConnection>(actionResults);
            data.Attempts = _connectionAttempts;
            // Called from ValidateConnectionAsync which is always memory
            data.CredentialSourceId = CredentialSourceId.Memory;
            // When CodeWhisperer is implemented this will be variable
            data.FeatureId = FeatureId.AwsExplorer;
            // The access key ID/secret key fields can be detected from AmazonServiceException.ErrorCode where
            // InvalidClientTokenId is a bad access key ID and SignatureDoesNotMatch is a bad secret key.
            // This currently throws in AwsConnectionManager.PerformValidation.  There isn't a feasible way to
            // get that information from there to here with the way the credential subsystem is written today.
            // It's also not practical to emit this metric from that location as other fields that are known in
            // this context are unavailable there.  InvalidInputFields cannot be set at this time.
            data.InvalidInputFields = "";
            data.IsAggregated = false;
            // Same explanation as InvalidInputFields above.
            data.Reason = "";
            data.Result = actionResults.AsTelemetryResult();
            data.Source = saveMetricSource?.Location ?? MetadataValue.NotSet;

            ToolkitContext.TelemetryLogger.RecordAuthAddConnection(data);
        }

        public void RecordAuthAddedConnectionsMetric(ActionResults actionResults, int newConnectionCount, IEnumerable<string> newEnabledConnections)
        {
            var saveMetricSource = _addEditProfileWizardHost.SaveMetricSource;
            var authConnectionsCount = 0;
            var enabledAuthConnections = new HashSet<string>();

            foreach (var credId in ToolkitContext.ConnectionManager.CredentialManager.GetCredentialIdentifiers())
            {
                ++authConnectionsCount;

                switch (ToolkitContext.CredentialSettingsManager.GetProfileProperties(credId).GetCredentialType())
                {
                    // This may change as bearer token handling evolves
                    case Credentials.Utils.CredentialType.BearerToken:
                        enabledAuthConnections.Add(EnabledAuthConnectionTypes.BuilderIdCodeCatalyst);
                        break;
                    case Credentials.Utils.CredentialType.SsoProfile:
                        enabledAuthConnections.Add(EnabledAuthConnectionTypes.IamIdentityCenterAwsExplorer);
                        break;
                    case Credentials.Utils.CredentialType.StaticProfile:
                        enabledAuthConnections.Add(EnabledAuthConnectionTypes.StaticCredentials);
                        break;
                }
            }

            var data = CreateMetricData<AuthAddedConnections>(actionResults);
            data.Attempts = _connectionAttempts;
            data.AuthConnectionsCount = authConnectionsCount;
            data.EnabledAuthConnections = BuildEnabledAuthConnectionsString(enabledAuthConnections);
            // SSO for AWS Explorer allows multiple roles to be added at once, so this isn't always one
            data.NewAuthConnectionsCount = newConnectionCount;
            // As this is called when adding a connection(s) it will always only contain one type
            data.NewEnabledAuthConnections = BuildEnabledAuthConnectionsString(newEnabledConnections);
            data.Result = actionResults.AsTelemetryResult();
            data.Source = saveMetricSource?.Location ?? MetadataValue.NotSet;

            ToolkitContext.TelemetryLogger.RecordAuthAddedConnections(data);
        }

        private string BuildEnabledAuthConnectionsString(IEnumerable<string> enabledAuthConnections)
        {
            return string.Join(",", enabledAuthConnections.OrderBy(s => s));
        }
    }

    // See AuthFormId for values https://github.com/aws/aws-toolkit-vscode/blob/master/src/auth/ui/vue/authForms/types.ts
    public static class EnabledAuthConnectionTypes
    {
        public const string StaticCredentials = "credentials";

        public const string BuilderIdCodeWhisperer = "builderIdCodeWhisperer";

        public const string BuilderIdCodeCatalyst = "builderIdCodeCatalyst";

        public const string IamIdentityCenterCodeWhisperer = "identityCenterCodeWhisperer";

        public const string IamIdentityCenterAwsExplorer = "identityCenterExplorer";
    }
}
