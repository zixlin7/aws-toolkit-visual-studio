using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class ECSServicePageController : IAWSWizardPageController
    {
        private ECSServicePage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string Cluster { get; private set; }

        public string PageDescription
        {
            get
            {
                return "";
            }
        }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public string PageTitle
        {
            get
            {
                return "Amazon ECS Service";
            }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            var cluster = HostingWizard[PublishContainerToAWSWizardProperties.Cluster] as string;
            if(!string.Equals(cluster, this.Cluster))
            {
                this.Cluster = cluster;
                this._pageUI.InitializeWithNewCluster();
            }

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
                if (mode != Constants.DeployMode.DeployToECSCluster)
                {
                    return true;
                }

                if (HostingWizard[PublishContainerToAWSWizardProperties.IsExistingCluster] is bool)
                {
                    var isExistingService = (bool)HostingWizard[PublishContainerToAWSWizardProperties.IsExistingCluster];
                    if (isExistingService)
                        return false;
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
                return mode == Constants.DeployMode.DeployToECSCluster;
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

        bool StorePageData()
        {
            if (_pageUI == null)
                return false;

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

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
