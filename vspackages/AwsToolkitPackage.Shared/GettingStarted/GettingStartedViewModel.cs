using System;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public class GettingStartedViewModel : BaseModel, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedViewModel));

        private readonly ToolkitContext _toolkitContext;

        private readonly IAwsConnectionManager _connectionManager;

        public CredentialProfileFormViewModel CredentialProfileFormViewModel { get; private set; }

        #region ActiveCard
        internal const string AddProfileCardName = "AddProfileCard";

        internal const string GettingStartedCardName = "GettingStartedCard";

        private string _activeCard;

        public string ActiveCard
        {
            get => _activeCard;
            set => SetProperty(ref _activeCard, value);
        }
        #endregion

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

        #region Status
        private bool? _status;

        public bool? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private void _connectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            Status = e.State.IsTerminal ? ConnectionState.IsValid(e.State) : (bool?) null;
        }
        #endregion

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

        public ICommand OpenAwsExplorerAsyncCommand { get; }

        public ICommand OpenUsingToolkitDocsCommand { get; }

        public ICommand OpenDeployLambdaDocsCommand { get; }

        public ICommand OpenDeployBeanstalkDocsCommand { get; }

        public ICommand OpenDevBlogCommand { get; }

        public ICommand OpenPrivacyPolicyCommand { get; }

        public GettingStartedViewModel(ToolkitContext toolkitContext)
            : this(
                  toolkitContext,
                  new AwsConnectionManager(
                    toolkitContext.CredentialManager,
                    toolkitContext.TelemetryLogger,
                    toolkitContext.RegionProvider,
                    new AppDataToolkitSettingsRepository()))
        {

        }

        internal GettingStartedViewModel(ToolkitContext toolkitContext, IAwsConnectionManager connectionManager)
        {
            _toolkitContext = toolkitContext;
            _connectionManager = connectionManager;

            CredentialProfileFormViewModel = new CredentialProfileFormViewModel(_toolkitContext);

            Func<string, ICommand> openUrl = url => OpenUrlCommandFactory.Create(_toolkitContext, url);

            OpenAwsExplorerAsyncCommand = new OpenAwsExplorerCommand(_toolkitContext);
            OpenUsingToolkitDocsCommand = OpenUserGuideCommand.Create(_toolkitContext);
            OpenDeployLambdaDocsCommand = openUrl("https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/lambda-cli-publish.html");
            OpenDeployBeanstalkDocsCommand = openUrl("https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.html");
            OpenDevBlogCommand = openUrl("https://aws.amazon.com/blogs/developer/category/net/");
            OpenPrivacyPolicyCommand = openUrl("https://aws.amazon.com/privacy/");

            Initialize();
        }

        private void Initialize()
        {
            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            _connectionManager.ConnectionStateChanged += _connectionManager_ConnectionStateChanged;

            var credId = GetDefaultCredentialIdentifier();
            if (credId != null)
            {
                ShowGettingStartedCard(credId);
                return;
            }

            CredentialProfileFormViewModel.CredentialProfileSaved += CredentialProfileFormViewModel_CredentialProfileSaved;
            ActiveCard = AddProfileCardName;
        }

        private void ChangeConnectionSettings(IAwsConnectionManager connectionManager, ICredentialIdentifier credentialIdentifier, ProfileProperties profileProperties)
        {
            connectionManager.ChangeConnectionSettings(credentialIdentifier, _toolkitContext.RegionProvider.GetRegion(profileProperties.Region ?? ToolkitRegion.DefaultRegionId));
        }

        private void ShowGettingStartedCard(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = _toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);

            CredentialTypeName = profileProperties.GetCredentialType().GetDescription();
            CredentialName = credentialIdentifier.ProfileName;
            ActiveCard = GettingStartedCardName;

            ChangeConnectionSettings(_connectionManager, credentialIdentifier, profileProperties);
        }

        private void CredentialProfileFormViewModel_CredentialProfileSaved(object sender, CredentialProfileFormViewModel.CredentialProfileSavedEventArgs e)
        {
            CredentialProfileFormViewModel.CredentialProfileSaved -= CredentialProfileFormViewModel_CredentialProfileSaved;
            ShowGettingStartedCard(e.CredentialIdentifier);
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _connectionManager.ConnectionStateChanged -= _connectionManager_ConnectionStateChanged;

                if (CredentialProfileFormViewModel != null)
                {
                    CredentialProfileFormViewModel.Dispose();
                    CredentialProfileFormViewModel = null;
                }
            }
        }

        private ICredentialIdentifier GetDefaultCredentialIdentifier()
        {
            var credIds = _toolkitContext.CredentialManager.GetCredentialIdentifiers().Where(ci =>
                ci.FactoryId == SDKCredentialProviderFactory.SdkProfileFactoryId ||
                ci.FactoryId == SharedCredentialProviderFactory.SharedProfileFactoryId);

            return credIds.FirstOrDefault(ci => ci.ProfileName == "default") ?? credIds.FirstOrDefault();
        }
    }
}
