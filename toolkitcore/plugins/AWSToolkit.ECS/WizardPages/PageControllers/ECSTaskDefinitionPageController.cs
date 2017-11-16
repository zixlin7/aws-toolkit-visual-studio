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
    public class ECSTaskDefinitionPageController : IAWSWizardPageController
    {
        private ECSTaskDefinitionPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get
            {
                return "Task Definition defines the parameters for how the application will run within its Docker container.";
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
                return "ECS Task Definition";
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
            _pageUI.PageActivated();
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ECSTaskDefinitionPage(this);
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
                if(mode != Constants.DeployMode.DeployService && mode != Constants.DeployMode.ScheduleTask && mode != Constants.DeployMode.RunTask)
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
                return mode == Constants.DeployMode.DeployService || mode == Constants.DeployMode.ScheduleTask || mode == Constants.DeployMode.RunTask;
            }

            return false;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
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

            if(_pageUI.CreateTaskDefinition)
                HostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition] = _pageUI.NewTaskDefinitionName;
            else
                HostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition] = _pageUI.TaskDefinition;

            if(this._pageUI.CreateContainer)
                HostingWizard[PublishContainerToAWSWizardProperties.Container] = _pageUI.NewContainerName;
            else
                HostingWizard[PublishContainerToAWSWizardProperties.Container] = _pageUI.Container;

            if(this.HostingWizard.IsFargateLaunch())
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTaskExecutionRole] = _pageUI.CreateNewTaskExecutionRole;
                HostingWizard[PublishContainerToAWSWizardProperties.TaskExecutionRole] = _pageUI.TaskExecutionRole;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTaskExecutionRole] = false;
                HostingWizard[PublishContainerToAWSWizardProperties.TaskExecutionRole] = null;
            }

            HostingWizard[PublishContainerToAWSWizardProperties.TaskRole] = _pageUI.SelectedRole;
            HostingWizard[PublishContainerToAWSWizardProperties.TaskRoleManagedPolicy] = _pageUI.SelectedManagedPolicy;

            HostingWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit] = _pageUI.MemoryHardLimit;
            HostingWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit] = _pageUI.MemorySoftLimit;
            HostingWizard[PublishContainerToAWSWizardProperties.PortMappings] = _pageUI.PortMappings.ToList();
            HostingWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] = _pageUI.EnvironmentVariables.ToList();

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
