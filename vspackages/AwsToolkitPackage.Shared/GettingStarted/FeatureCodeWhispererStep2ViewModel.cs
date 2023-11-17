using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Urls;

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

        public async Task ExecuteSignInWithAwsBuilderIdCommandAsync(object parameter)
        {
            var credId = ToolkitContext.CredentialManager.GetCredentialIdentifierById(
                new SonoCredentialIdentifier(SonoCredentialProviderFactory.CodeWhispererProfileName).Id);
            var ssoRegion = ToolkitContext.RegionProvider.GetRegion(SonoProperties.DefaultTokenProviderRegion.SystemName);

            await Task.Run(async () =>
            {
                try
                {
                    await ToolkitContext.CredentialManager.GetToolkitCredentials(credId, ssoRegion)
                        .GetTokenProvider().TryResolveTokenAsync();
                }
                catch (Exception ex)
                {
                    TryInvalidateCredential(credId);

                    var msg = "Unable to resolve AWS Builder ID token.  Try to login again.";
                    ToolkitContext.ToolkitHost.ShowError(msg);
                    _logger.Error(msg, ex);
                }
            });

            _host.ShowCompleted(credId);
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
