using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.Urls;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    internal class FeatureCodeWhispererStep2ViewModel : ViewModel
    {
        private ICommand _openAuthProvidersLearnMoreCommand;

        public ICommand OpenAuthProvidersLearnMoreCommand
        {
            get => _openAuthProvidersLearnMoreCommand;
            private set => SetProperty(ref _openAuthProvidersLearnMoreCommand, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            OpenAuthProvidersLearnMoreCommand = OpenUrlCommandFactory.Create(ToolkitContext, AwsUrls.UserGuideAuth);
        }
    }
}
