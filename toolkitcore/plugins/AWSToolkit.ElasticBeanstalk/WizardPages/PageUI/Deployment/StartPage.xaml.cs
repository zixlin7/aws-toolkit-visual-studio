using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_Region = "Region";
        public static readonly string uiProperty_Account = "Account";
        public static readonly string uiProperty_Accounts = "Accounts";
        public static readonly string uiProperty_DeploymentMode = "DeploymentMode"; // deploy new vs redeploy
        public static readonly string uiProperty_ExistingDeployments = "ExistingDeployments";
        public static readonly string uiProperty_SelectedDeployment = "SelectedDeployment";

        public StartPage()
        {
            InitializeComponent();
            this._accountSelector.PropertyChanged += _accountSelector_PropertyChanged;
            DataContext = this;
        }

        public StartPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void Initialize(AccountViewModel account)
        {
            this._accountSelector.Initialize(account, RegionEndPointsManager.GetInstance().GetDefaultRegionEndPoints(), new string[] { RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME });
            this._accountSelector.IsEnabled = true;
        }

        private void _accountSelector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Region);
            NotifyPropertyChanged(uiProperty_Account);
        }

        AWSViewModel _rootViewModel;
        public AWSViewModel RootViewModel
        {
            get => IsInitialized ? _rootViewModel : null;
            set
            {
                this._rootViewModel = value;
                this._accountSelector.IsEnabled = this.Accounts.Count != 0;
            }
        }

        ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                if (RootViewModel == null)
                    return null;

                return this._accounts;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get => this._accountSelector.SelectedAccount;
            protected set { if (IsInitialized) this._accountSelector.SelectedAccount = value; }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get => this._accountSelector.SelectedRegion;
            set => this._accountSelector.SelectedRegion = value;
        }

        private ObservableCollection<DeployedApplicationModel> _existingDeployments;

        public ObservableCollection<DeployedApplicationModel> ExistingDeployments => _existingDeployments;

        public void LoadAvailableDeployments(ICollection<DeployedApplicationModel> deployments)
        {
            _existingDeployments = new ObservableCollection<DeployedApplicationModel>(deployments);
            NotifyPropertyChanged(uiProperty_ExistingDeployments);
            _btnRedeploy.IsEnabled = deployments != null && deployments.Count > 0;
        }

        public bool RedeploySelected => _btnRedeploy.IsChecked == true;

        // returns the selected environment to redeploy to - if the user has selected
        // an application (root) tree item, null is returned, allowing us to disable
        // forward navigation until an env is selected
        public DeployedApplicationModel SelectedDeployment
        {
            get
            {
                if (_existingDeploymentsTree == null || _existingDeploymentsTree.SelectedItem == null)
                    return null;

                var deployment = _existingDeploymentsTree.SelectedItem as DeployedApplicationModel;
                if (deployment == null)
                {
                    var environment = _existingDeploymentsTree.SelectedItem as EnvironmentDescription;
                    deployment = FindMatchingDeploymentModel(environment.ApplicationName);
                }
                return deployment;
            }
        }

        public bool LockToNewWizard
        {
            set => _btnUseLegacyWizard.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
        }


        private void UseLegacyWizard_OnClick(object sender, RoutedEventArgs e)
        {
            PageController.HostingWizard.CancelRun(DeploymentWizardProperties.SeedData.propkey_LegacyDeploymentMode, true);    
        }

        // this is used to wire an environment selection into the overall DeployedApplicationModel
        // attached to a treeitem root
        private void _existingDeploymentsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var environment = e.NewValue as EnvironmentDescription;
            if (!this._btnRedeploy.IsChecked.GetValueOrDefault())
                this._btnRedeploy.IsChecked = true;

            if (environment != null)
            {
                var deployment = FindMatchingDeploymentModel(environment.ApplicationName);
                deployment.SelectedEnvironmentName = environment.EnvironmentName;
            }
            else
            {
                // if the user has selected an app with more than one environment, clear out selected env
                var deployment = e.NewValue as DeployedApplicationModel;
                if (deployment != null && deployment.Environments.Count > 1)
                    deployment.SelectedEnvironmentName = null;
            }

            NotifyPropertyChanged(uiProperty_SelectedDeployment);
        }

        private DeployedApplicationModel FindMatchingDeploymentModel(string applicationName)
        {
            return _existingDeployments.FirstOrDefault(d => d.ApplicationName.Equals(applicationName, StringComparison.Ordinal));
        }

        private void DeploymentModeButton_OnChecked(object sender, RoutedEventArgs e)
        {
            // if the user is toggling between create-new and redeploy, clean out any
            // selected environment otherwise when they come back to redeploy, the Next
            // button stays enabled but there is no selected environment (or the one
            // that is is out of date and not reflected in the UI)
            if (SelectedDeployment != null)
                SelectedDeployment.SelectedEnvironmentName = null;

            NotifyPropertyChanged(uiProperty_DeploymentMode);
        }
    }
}
