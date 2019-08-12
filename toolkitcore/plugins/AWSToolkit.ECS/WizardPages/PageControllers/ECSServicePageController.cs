using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class ECSServicePageController : IAWSWizardPageController
    {
        private ECSServicePage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string Cluster { get; private set; }

        public string PageDescription => "Choose the number of instances of the service and how the instances should be deployed.";

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle => "Service Configuration";

        public string ShortPageTitle => null;

        public bool AllowShortCircuit()
        {
            return true;
        }

        public void ResetPage()
        {
            this._pageUI = null;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            string clusterName = null;
            if(!(HostingWizard[PublishContainerToAWSWizardProperties.CreateNewCluster] is bool) || 
                !(bool)HostingWizard[PublishContainerToAWSWizardProperties.CreateNewCluster])
            {
                clusterName = HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string;
            }

            if (!string.Equals(clusterName, this.Cluster))
            {
                this.Cluster = clusterName;
                this._pageUI.InitializeWithNewCluster();
            }
            this._pageUI.PageActivated();


            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ECSServicePage(this);
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
            }

            return _pageUI;
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                return StorePageData();

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            if (HostingWizard.IsPropertySet(PublishContainerToAWSWizardProperties.DeploymentMode))
            {
                var mode = (Constants.DeployMode)HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode];
                if (mode != Constants.DeployMode.DeployService)
                {
                    return true;
                }
            }

            // don't stand in the way of our previous sibling pages!
            return IsForwardsNavigationAllowed;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (HostingWizard.IsPropertySet(PublishContainerToAWSWizardProperties.DeploymentMode))
            {
                var mode = (Constants.DeployMode)HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode];
                return mode == Constants.DeployMode.DeployService;
            }

            return false;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, QueryFinishButtonEnablement());
        }

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                return _pageUI.AllRequiredFieldsAreSet;
            }
        }

        string _lastServiceName;
        bool _lastCreateService;

        bool StorePageData()
        {
            if (_pageUI == null)
                return false;

            bool resetForwardPages = false;
            if(_lastCreateService != this._pageUI.CreateNewService || 
                string.Equals(_lastServiceName, _pageUI.Service))
            {
                resetForwardPages = true;
            }
            _lastCreateService = this._pageUI.CreateNewService;
            _lastServiceName = _pageUI.Service;

            HostingWizard[PublishContainerToAWSWizardProperties.CreateNewService] = this._pageUI.CreateNewService;
            HostingWizard[PublishContainerToAWSWizardProperties.Service] = this._pageUI.CreateNewService ? _pageUI.NewServiceName : _pageUI.Service;
            HostingWizard[PublishContainerToAWSWizardProperties.DesiredCount] = this._pageUI.DesiredCount;
            HostingWizard[PublishContainerToAWSWizardProperties.MinimumHealthy] = this._pageUI.MinimumHealthy;
            HostingWizard[PublishContainerToAWSWizardProperties.MaximumPercent] = this._pageUI.MaximumPercent;

            if (this._pageUI.IsPlacementTemplateEnabled)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.PlacementConstraints] = this._pageUI.PlacementTemplate.PlacementConstraints;
                HostingWizard[PublishContainerToAWSWizardProperties.PlacementStrategy] = this._pageUI.PlacementTemplate.PlacementStrategy;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.PlacementConstraints] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.PlacementStrategy] = null;
            }

            if (resetForwardPages)
            {
                this.HostingWizard.NotifyForwardPagesReset(this);
            }

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
