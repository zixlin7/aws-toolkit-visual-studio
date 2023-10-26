using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public class GettingStartedCompletedViewModel : ViewModel, IGettingStartedCompleted
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedCompletedViewModel));

        private bool? _status;

        public bool? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _credentialTypeName;

        public string CredentialTypeName
        {
            get => _credentialTypeName;
            set => SetProperty(ref _credentialTypeName, value);
        }

        private string _credentialName;

        public string CredentialName
        {
            get => _credentialName;
            set => SetProperty(ref _credentialName, value);
        }

        #region CollectAnalytics
        public bool CollectAnalytics
        {
            get
            {
                try
                {
                    return ToolkitSettings.Instance.TelemetryEnabled;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                return ToolkitSettings.DefaultValues.TelemetryEnabled;
            }
            set
            {
                try
                {
                    if (ToolkitSettings.Instance.TelemetryEnabled != value)
                    {
                        ToolkitSettings.Instance.TelemetryEnabled = value;
                        NotifyPropertyChanged(nameof(CollectAnalytics));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }
        #endregion

        private ICommand _openAwsExplorerAsyncCommand;

        public ICommand OpenAwsExplorerAsyncCommand
        {
            get => _openAwsExplorerAsyncCommand;
            private set => SetProperty(ref _openAwsExplorerAsyncCommand, value);
        }

        private ICommand _openDeployBeanstalkDocsCommand;

        public ICommand OpenDeployBeanstalkDocsCommand
        {
            get => _openDeployBeanstalkDocsCommand;
            private set => SetProperty(ref _openDeployBeanstalkDocsCommand, value);
        }

        private ICommand _openDeployLambdaDocsCommand;

        public ICommand OpenDeployLambdaDocsCommand
        {
            get => _openDeployLambdaDocsCommand;
            private set => SetProperty(ref _openDeployLambdaDocsCommand, value);
        }

        private ICommand _openDevBlogCommand;

        public ICommand OpenDevBlogCommand
        {
            get => _openDevBlogCommand;
            private set => SetProperty(ref _openDevBlogCommand, value);
        }

        private ICommand _openLogsCommand;

        public ICommand OpenLogsCommand
        {
            get => _openLogsCommand;
            private set => SetProperty(ref _openLogsCommand, value);
        }

        private ICommand _openPrivacyPolicyCommand;

        public ICommand OpenPrivacyPolicyCommand
        {
            get => _openPrivacyPolicyCommand;
            private set => SetProperty(ref _openPrivacyPolicyCommand, value);
        }

        private ICommand _openTelemetryDisclosureCommand;

        public ICommand OpenTelemetryDisclosureCommand
        {
            get => _openTelemetryDisclosureCommand;
            private set => SetProperty(ref _openTelemetryDisclosureCommand, value);
        }

        private ICommand _openUsingToolkitDocsCommand;

        public ICommand OpenUsingToolkitDocsCommand
        {
            get => _openUsingToolkitDocsCommand;
            private set => SetProperty(ref _openUsingToolkitDocsCommand, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            OpenAwsExplorerAsyncCommand = new OpenAwsExplorerCommand(ToolkitContext);
            OpenLogsCommand = new OpenToolkitLogsCommand(ToolkitContext);
            OpenUsingToolkitDocsCommand = OpenUserGuideCommand.Create(ToolkitContext);

            Func<string, ICommand> openUrl = url => OpenUrlCommandFactory.Create(ToolkitContext, url);
            OpenPrivacyPolicyCommand = openUrl(AwsUrls.PrivacyPolicy);
            OpenTelemetryDisclosureCommand = openUrl(AwsUrls.TelemetryDisclosure);
            OpenDeployLambdaDocsCommand = openUrl(AwsUrls.DeployLambdaDocs);
            OpenDeployBeanstalkDocsCommand = openUrl(AwsUrls.DeployBeanstalkDocs);
            OpenDevBlogCommand = openUrl(AwsUrls.DevBlog);
        }

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<IGettingStartedCompleted>(this);
        }
    }
}
