using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Tasks;
using Amazon.Runtime;
using Amazon.SSOOIDC.Model;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoConnectingStepViewModel : StepViewModel, IResolveAwsToken
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoConnectingStepViewModel));

        private IConfigurationDetails _configDetails => ServiceProvider.RequireService<IConfigurationDetails>(CredentialType.SsoProfile.ToString());

        #region CancelConnecting

        private CancellationTokenSource _cancellationTokenSource;

        public CancellationTokenSource CancellationTokenSource
        {
            get => _cancellationTokenSource;
            set => SetProperty(ref _cancellationTokenSource, value);
        }

        private ICommand _cancelConnectingCommand;

        public ICommand CancelConnectingCommand
        {
            get => _cancelConnectingCommand;
            private set => SetProperty(ref _cancelConnectingCommand, value);
        }

        private void CancelConnecting(object parameter)
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource = null;

                _addEditProfileWizard.CurrentStep = WizardStep.Configuration;
                _addEditProfileWizard.InProgress = false;
            }
        }
        #endregion

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<IResolveAwsToken>(this);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            CancelConnectingCommand = new RelayCommand(CancelConnecting);
        }

        public override async Task ViewShownAsync()
        {
            await base.ViewShownAsync();

            // NOTE - We need a way through the UX for users to determine bearer token or
            // AWS credentials, but for now we'll assume if the feature is CodeWhisperer
            // it's a bearer token.  When another feature/service is added, this will
            // need to change.
            _addEditProfileWizard.CurrentStep = await ResolveAwsTokenAsync() == null ?
                WizardStep.Configuration :
                _addEditProfileWizard.FeatureType == FeatureType.AwsExplorer ?
                    WizardStep.SsoAwsCredentialConnected :
                    WizardStep.SsoBearerTokenConnected;
        }

        public Task<AWSToken> ResolveAwsTokenAsync()
        {
            return ResolveAwsTokenAsync(new CancellationTokenSource());
        }

        // Overload to support unit tests
        internal Task<AWSToken> ResolveAwsTokenAsync(CancellationTokenSource cancellationTokenSource)
        {
            var p = _configDetails.ProfileProperties;

            p.SsoSession = p.Name;

            // When CodeWhisperer work causes refactoring of Sono* to generic classes, this code can be refactored to
            // go through ProfileCredentialProviderFactory.CreateToolkitCredentials.
            var ssoRegion = RegionEndpoint.GetBySystemName(p.SsoRegion);

            var credId = new MemoryCredentialIdentifier(p.Name);

            // SEPs SSO Credential Provider, SSO Login Token Flow, and Bearer Token Authorization and Token Providers
            // state that the OIDC client be created in the region provided by sso_region.
            var builder = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(credId)
                .WithIsBuilderId(false)
                .WithOidcRegion(ssoRegion)
                .WithSessionName(p.SsoSession)
                .WithStartUrl(p.SsoStartUrl)
                .WithTokenProviderRegion(ssoRegion)
                .WithToolkitShell(ToolkitContext.ToolkitHost);

            if (_addEditProfileWizard.FeatureType == FeatureType.CodeWhisperer)
            {
                builder.WithScopes(p.SsoRegistrationScopes);
            }

            var provider = builder.Build();

            return Task.Run(async () =>
            {
                _addEditProfileWizard.InProgress = true;

                CancellationTokenSource = cancellationTokenSource;
                var cancellationToken = cancellationTokenSource.Token;

                // CancellationToken doesn't make it to CoreAmazonSSOOIDC.PollForSsoTokenAsync, so this won't actually stop
                // the polling loop, but it is handled in other calls in the chain, so doesn't hurt to pass it.
                var resolverTask = provider.TryResolveTokenAsync(cancellationToken);
                try
                {
                    while (true)
                    {
                        var delayTask = Task.Delay(TimeSpan.FromSeconds(1));
                        var finishedTask = await Task.WhenAny(resolverTask, delayTask);

                        if (finishedTask == resolverTask)
                        {
                            await resolverTask;
                            break;
                        }
                        else
                        {
                            await delayTask;

                            if (cancellationToken.IsCancellationRequested)
                            {
                                resolverTask.LogExceptionAndForget();
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    }
                }
                catch (UserCanceledException)
                {
                    // Just ignore if the user cancelled, any exception here will revert back to the SSO configuration details screen
                }
                catch (InvalidRequestException)
                {
                    ToolkitContext.ToolkitHost.ShowError("Unable to connect.  Verify SSO Start URL is correct.");
                }
                catch (InvalidGrantException)
                {
                    ToolkitContext.ToolkitHost.ShowError("Invalid grant.  Verify SSO Start URL is correct.");
                }
                catch (Exception ex)
                {
                    TryInvalidateCredential(credId);

                    var msg = "Unable to resolve IAM Identity Center token.";
                    ToolkitContext.ToolkitHost.ShowError(msg);
                    _logger.Error(msg, ex);
                }
                finally
                {
                    _addEditProfileWizard.InProgress = false;
                }

                return !cancellationTokenSource.IsCancellationRequested &&
                    resolverTask.Status == TaskStatus.RanToCompletion &&
                    resolverTask.Result?.Success == true ?
                    resolverTask.Result.Value :
                    null;
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
    }
}
