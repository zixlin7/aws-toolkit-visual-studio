using System;
using System.IO;
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
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;
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

        public event EventHandler<ConnectionSettingsChangeArgs> ConnectionSettingsChanged;

        protected void OnConnectionSettingsChanged(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            OnConnectionSettingsChanged(new ConnectionSettingsChangeArgs()
            {
                CredentialIdentifier = credentialIdentifier,
                Region = region
            });
        }

        protected virtual void OnConnectionSettingsChanged(ConnectionSettingsChangeArgs e)
        {
            ConnectionSettingsChanged?.Invoke(this, e);
        }

        public async Task SaveAsync(ProfileProperties profileProperties, CredentialFileType fileType, bool changeConnectionSettings = true)
        {
            Status = null;
            InProgress = true;

            if (!await ValidateConnectionAsync(profileProperties))
            {
                Status = false;
                InProgress = false;
                return;
            }

            ActionResults actionResults = null;
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
                        OnConnectionSettingsChanged(credId, region);
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
        }

        private int _connectionAttempts = 0;

        internal async Task<bool> ValidateConnectionAsync(ProfileProperties profileProperties)
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

                return actionResults.Success;
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

        public BaseMetricSource SaveMetricSource { get; set; }

        private T CreateMetricData<T>(ActionResults actionResults) where T : BaseTelemetryEvent, new()
        {
#if DEBUG
            if (SaveMetricSource == null)
            {
                throw new InvalidOperationException($"{nameof(SaveMetricSource)} must be set by wizard host.");
            }
#endif
            var accountId = ToolkitContext.ConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet;

            return actionResults.CreateMetricData<T>(accountId, MetadataValue.NotApplicable);
        }

        private void RecordAwsModifyCredentialsMetric(ActionResults actionResults)
        {
            var data = CreateMetricData<AwsModifyCredentials>(actionResults);
            data.Result = actionResults.AsTelemetryResult();
            data.CredentialModification = CredentialModification.Add;
            data.Source = SaveMetricSource?.Location ?? MetadataValue.NotSet;
            data.ServiceType = SaveMetricSource?.Service ?? MetadataValue.NotSet;

            ToolkitContext.TelemetryLogger.RecordAwsModifyCredentials(data);
        }

        private void RecordAuthAddConnectionMetric(ActionResults actionResults)
        {
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
            data.Source = SaveMetricSource?.Location ?? MetadataValue.NotSet;

            ToolkitContext.TelemetryLogger.RecordAuthAddConnection(data);
        }

        // TODO Add as part of IDE-11500
        //    private void RecordAuthAddedConnectionsMetric(ActionResults actionResults)
        //    {
        //        var data = CreateMetricData<AuthAddedConnections>(actionResults);
        //        data.Attempts = _connectionAttempts;
        //        data.AuthConnectionsCount = 0;
        //        data.EnabledAuthConnections = "";
        //        data.NewAuthConnectionsCount = 0;
        //        data.NewEnabledAuthConnections = "";
        //        data.Result = actionResults.AsTelemetryResult();
        //        data.Source = SaveMetricSource?.Location ?? MetadataValue.NotSet;

        //        ToolkitContext.TelemetryLogger.RecordAuthAddedConnections(data);
        //    }
    }
}
