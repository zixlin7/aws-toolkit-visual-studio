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
    public class ScheduleTaskPageController : IAWSWizardPageController
    {
        private ScheduleTaskPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string Cluster { get; private set; }

        public string PageDescription
        {
            get
            {
                return "Run Amazon ECS tasks on a cron like schedule using CloudWatch Events rules and targets.";
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
                return "Scheduled Task Configuration";
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
            var cluster = HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string;
            if (!string.Equals(cluster, this.Cluster))
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
                _pageUI = new ScheduleTaskPage(this);
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
                if (mode != Constants.DeployMode.ScheduleTask)
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
                return mode == Constants.DeployMode.ScheduleTask;
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

            HostingWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleName] = this._pageUI.CreateNewScheduleRule ? _pageUI.NewScheduleRule : _pageUI.ScheduleRule;
            HostingWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleTarget] = this._pageUI.CreateNewTarget ? _pageUI.NewTarget : _pageUI.Target;
            HostingWizard[PublishContainerToAWSWizardProperties.DesiredCount] = this._pageUI.DesiredCount;

            HostingWizard[PublishContainerToAWSWizardProperties.CreateCloudWatchEventIAMRole] = _pageUI.CreateCloudWatchEventIAMRole;
            if (!_pageUI.CreateCloudWatchEventIAMRole)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CloudWatchEventIAMRole] = this._pageUI.CloudWatchEventIAMRoleArn;
            }

            if (this._pageUI.IsRunTypeCronExpression.GetValueOrDefault())
            {
                HostingWizard[PublishContainerToAWSWizardProperties.ScheduleExpression] = this._pageUI.CronExpression;
            }
            else
            {
                var expression = $"rate({this._pageUI.RunIntervalValue} {this._pageUI.RunIntervalUnit.GetUnitName(this._pageUI.RunIntervalValue.Value)})";
                HostingWizard[PublishContainerToAWSWizardProperties.ScheduleExpression] = expression;
            }


            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
