using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;
using Amazon.AWSToolkit.Account;
using System.ComponentModel;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PushImageToECRPage.xaml
    /// </summary>
    public partial class PushImageToECRPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PushImageToECRPage));

        public PushImageToECRPageController PageController { get; private set; }

        public PushImageToECRPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public PushImageToECRPage(PushImageToECRPageController pageController)
            : this()
        {
            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            var userAccount = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var regionEndpoints = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            this._ctlAccountAndRegion.Initialize(userAccount, regionEndpoints, new string[] { Constants.ECR_ENDPOINT_LOOKUP });
            this._ctlAccountAndRegion.PropertyChanged += _ctlAccountAndRegion_PropertyChanged;

            this._ctlConfigurationPicker.Items.Add("Release");
            this._ctlConfigurationPicker.Items.Add("Debug");
            this.Configuration = "Release";

            var buildConfiguration = hostWizard[PublishContainerToAWSWizardProperties.Configuration] as string;
            if (!string.IsNullOrEmpty(buildConfiguration) && this._ctlConfigurationPicker.Items.Contains(buildConfiguration))
            {
                this.Configuration = buildConfiguration;
            }
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (this.SelectedAccount == null)
                    return false;
                if (this.SelectedRegion == null)
                    return false;
                if (string.IsNullOrWhiteSpace(this.DockerImageTag))
                    return false;
                if (string.IsNullOrWhiteSpace(this.Configuration))
                    return false;

                return true;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get
            {
                return _ctlAccountAndRegion.SelectedAccount;
            }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get
            {
                return _ctlAccountAndRegion.SelectedRegion;
            }
        }

        void _ctlAccountAndRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                return;

            PageController.HostingWizard.SetProperty(PublishContainerToAWSWizardProperties.UserAccount, this._ctlAccountAndRegion.SelectedAccount);
            PageController.HostingWizard.SetProperty(PublishContainerToAWSWizardProperties.Region, this._ctlAccountAndRegion.SelectedRegion);

        }

        string _configuration;
        public string Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                NotifyPropertyChanged("Configuration");
            }
        }

        string _dockerImageTag;
        public string DockerImageTag
        {
            get { return _dockerImageTag; }
            set
            {
                _dockerImageTag = value;
                NotifyPropertyChanged("DockerImageTag");
            }
        }
    }
}
