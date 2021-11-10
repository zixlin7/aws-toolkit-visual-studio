using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Publish.Banner;
using Amazon.AWSToolkit.Regions;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_Accounts = "Accounts";
        public static readonly string uiProperty_DeploymentMode = "DeploymentMode"; // deploy new vs redeploy
        public static readonly string uiProperty_ExistingDeployments = "ExistingDeployments";
        public static readonly string uiProperty_SelectedDeployment = "SelectedDeployment";

        public static readonly string ElasticBeanstalkServiceName = new AmazonElasticBeanstalkConfig().RegionEndpointServiceName;

        public AccountAndRegionPickerViewModel Connection { get; }

        public PublishBannerViewModel PublishBanner { get; }

        public StartPage(ToolkitContext toolkitContext, IAWSWizard wizard)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            Connection.SetServiceFilter(new List<string>() {ElasticBeanstalkServiceName});

            PublishBanner = PublishBannerViewModelFactory.Create(toolkitContext);
            PublishBanner.Origin = ElasticBeanstalkServiceName;
            new PublishBannerPropertyChangedHandler(PublishBanner, wizard);

            InitializeComponent();
            DataContext = this;
        }

        private void ConnectionChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(Connection));
        }

        public AccountViewModel SelectedAccount
        {
            get => Connection.Account;
            set
            {
                if (IsInitialized)
                {
                    Connection.Account = value;
                }
            }
        }

        public ToolkitRegion SelectedRegion
        {
            get => Connection.Region;
            set => Connection.Region = value;
        }

        public string SelectedRegionId
        {
            get => Connection.Region?.Id;
            set => Connection.SetRegion(value);
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
