using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Exceptions;
using Amazon.Runtime;
using Amazon.SSOOIDC.Model;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoConnectingStepViewModel : StepViewModel, IResolveAwsToken
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoConnectingStepViewModel));

        private ISsoProfilePropertiesProvider _propertiesProvider => ServiceProvider.RequireService<ISsoProfilePropertiesProvider>();

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

            _addEditProfileWizard.CurrentStep = await ResolveAwsTokenAsync() == null ?
                WizardStep.Configuration :
                WizardStep.SsoConnected;
        }

        public Task<AWSToken> ResolveAwsTokenAsync()
        {
            return ResolveAwsTokenAsync(new CancellationTokenSource());
        }

        // Overload to support unit tests
        internal Task<AWSToken> ResolveAwsTokenAsync(CancellationTokenSource cancellationTokenSource)
        {
            var p = _propertiesProvider.ProfileProperties;

            p.SsoSession = p.Name;

            // When CodeWhisperer work causes refactoring of Sono* to generic classes, this code can be refactored to
            // go through ProfileCredentialProviderFactory.CreateToolkitCredentials.
            var ssoRegion = RegionEndpoint.GetBySystemName(p.SsoRegion);

            // SEPs SSO Credential Provider, SSO Login Token Flow, and Bearer Token Authorization and Token Providers
            // state that the OIDC client be created in the region provided by sso_region.
            var provider = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(new MemoryCredentialIdentifier(p.Name))
                .WithIsBuilderId(false)
                .WithOidcRegion(ssoRegion)
                .WithSessionName(p.SsoSession)
                .WithStartUrl(p.SsoStartUrl)
                .WithTokenProviderRegion(ssoRegion)
                .WithToolkitShell(_toolkitContext.ToolkitHost)
                .Build();

            return Task.Run(async () =>
            {
                _addEditProfileWizard.InProgress = true;

                CancellationTokenSource = cancellationTokenSource;
                var cancellationToken = cancellationTokenSource.Token;

                // CancellationToken doesn't make it to CoreAmazonSSOOIDC.PollForSsoTokenAsync, so this won't actually stop
                // the polling loop, but it is handled in other calls in the chain, so doesn't hurt to pass it.
                var task = provider.TryResolveTokenAsync(cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await task;
                }
                catch (UserCanceledException)
                {
                    // Just ignore if the user cancelled, any exception here will revert back to the SSO configuration details screen
                }
                catch (InvalidRequestException)
                {
                    _toolkitContext.ToolkitHost.ShowError("Unable to connect.  Verify SSO Start URL is correct.");
                }
                catch (Exception ex)
                {
                    _logger.Error("Unable to resolve IAM Identity Center token.", ex);
                }
                finally
                {
                    _addEditProfileWizard.InProgress = false;
                }

                return !cancellationTokenSource.IsCancellationRequested &&
                    task.Status == TaskStatus.RanToCompletion &&
                    task.Result?.Success == true ?
                    task.Result.Value :
                    null;
            });
        }
    }
}
