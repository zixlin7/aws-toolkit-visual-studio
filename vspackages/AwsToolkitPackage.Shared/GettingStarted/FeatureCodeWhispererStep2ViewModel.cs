using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Urls;

using static Amazon.AWSToolkit.Util.TelemetryExtensionMethods;

using log4net;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    internal class FeatureCodeWhispererStep2ViewModel : ViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FeatureCodeWhispererStep2ViewModel));

        private IAddEditProfileWizardHost _host => ServiceProvider.RequireService<IAddEditProfileWizardHost>();

        private ICommand _openAuthProvidersLearnMoreCommand;

        public ICommand OpenAuthProvidersLearnMoreCommand
        {
            get => _openAuthProvidersLearnMoreCommand;
            private set => SetProperty(ref _openAuthProvidersLearnMoreCommand, value);
        }

        private ICommand _signInWithAwsBuilderIdCommand;

        public ICommand SignInWithAwsBuilderIdCommand
        {
            get => _signInWithAwsBuilderIdCommand;
            private set => SetProperty(ref _signInWithAwsBuilderIdCommand, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            OpenAuthProvidersLearnMoreCommand = OpenUrlCommandFactory.Create(ToolkitContext, AwsUrls.UserGuideAuth);
            SignInWithAwsBuilderIdCommand = new AsyncRelayCommand(ExecuteSignInWithAwsBuilderIdCommandAsync);
        }

        private int _connectionAttempts = 0;

        public async Task ExecuteSignInWithAwsBuilderIdCommandAsync(object parameter)
        {
            var credId = ToolkitContext.CredentialManager.GetCredentialIdentifierById(
                new SonoCredentialIdentifier(SonoCredentialProviderFactory.CodeWhispererProfileName).Id);
            var ssoRegion = ToolkitContext.RegionProvider.GetRegion(SonoProperties.DefaultTokenProviderRegion.SystemName);

            await Task.Run(async () =>
            {
                ++_connectionAttempts;

                var actionResults = ActionResults.CreateFailed();

                try
                {
                    if ((await ToolkitContext.CredentialManager.GetToolkitCredentials(credId, ssoRegion)
                        .GetTokenProvider().TryResolveTokenAsync()).Success)
                    {
                        actionResults = new ActionResults().WithSuccess(true);
                        _host.ShowCompleted(credId);
                    }
                    else
                    {
                        throw new Exception($"Unable to sign into AWS Builder ID.");
                    }
                }
                catch (Exception ex)
                {
                    actionResults = ActionResults.CreateFailed(ex);

                    TryInvalidateCredential(credId);

                    var msg = "Unable to resolve AWS Builder ID token.  Try to login again.";
                    ToolkitContext.ToolkitHost.ShowError(msg);
                    _logger.Error(msg, ex);
                }
                finally
                {
                    RecordAuthAddConnectionMetric(actionResults);   
                }
            });
        }

        private bool TryInvalidateCredential(ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                ToolkitContext.CredentialManager.Invalidate(credentialIdentifier);
                return true;
            }
            catch (Exception ex)
            {
                // Deliberately swallow the error as this is best effort, but write log
                // to help with diagnosing a problem if necessary.
                _logger.Error($"Failure trying to invalidate cached SSO token for {credentialIdentifier?.Id}.", ex);
                return false;
            }
        }

        public void RecordAuthAddConnectionMetric(ActionResults actionResults)
        {
            var saveMetricSource = _host.SaveMetricSource;

            var data = actionResults.CreateMetricData<AuthAddConnection>(MetadataValue.NotApplicable, MetadataValue.NotApplicable);
            data.Attempts = _connectionAttempts;
            data.CredentialSourceId = CredentialSourceId.AwsId;
            data.FeatureId = FeatureId.Codewhisperer;
            // Errors regarding bearer token logins:
            // This currently throws in AwsConnectionManager.PerformValidation.  There isn't a feasible way to
            // get that information from there to here with the way the credential subsystem is written today.
            // It's also not practical to emit this metric from that location as other fields that are known in
            // this context are unavailable there.  InvalidInputFields cannot be set at this time.
            data.InvalidInputFields = "";
            data.IsAggregated = false;
            data.Result = actionResults.AsTelemetryResult();
            data.Source = saveMetricSource?.Location ?? MetadataValue.NotSet;

            ToolkitContext.TelemetryLogger.RecordAuthAddConnection(data);
        }
    }
}
