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

using Amazon.ECS.Model;

using static Amazon.AWSToolkit.ECS.WizardPages.ECSWizardUtils;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class RunTaskPageController : IAWSWizardPageController
    {
        private RunTaskPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string Cluster { get; private set; }

        public string PageDescription
        {
            get
            {
                return "Choose the number instances of the task and how the instances should be deployed.";
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
                return "Task Configuration";
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

        public void ResetPage()
        {
            this._pageUI = null;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            _pageUI.PageActivated();
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new RunTaskPage(this);
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
                if (mode != Constants.DeployMode.RunTask)
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
                return mode == Constants.DeployMode.RunTask;
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

            HostingWizard[PublishContainerToAWSWizardProperties.TaskGroup] = this._pageUI.TaskGroup;
            HostingWizard[PublishContainerToAWSWizardProperties.DesiredCount] = this._pageUI.DesiredCount;

            if(this._pageUI.IsPlacementTemplateEnabled)
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
