﻿using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
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
    public class ECSClusterPageController : IAWSWizardPageController
    {
        private ECSClusterPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get
            {
                return "A Cluster is the logical grouping for the compute resources that will run your Docker images.";
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
                return "ECS Cluster Setup";
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
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ECSClusterPage(this);
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
                if (mode != Constants.DeployMode.DeployService && mode != Constants.DeployMode.ScheduleTask && mode != Constants.DeployMode.RunTask)
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

            if (this._pageUI.CreateNewCluster)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewCluster] = this._pageUI.CreateNewCluster;
                HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] = this._pageUI.NewClusterName;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewCluster] = this._pageUI.CreateNewCluster;
                HostingWizard[PublishContainerToAWSWizardProperties.ExistingCluster] = FetchExistingCluster(this._pageUI.Cluster);
                HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] = this._pageUI.Cluster;
            }

            HostingWizard[PublishContainerToAWSWizardProperties.ExistingCluster] = this._pageUI.LaunchType.Value;
            if(this._pageUI.LaunchType == Amazon.ECS.LaunchType.FARGATE)
            {
                if (this._pageUI.CreateNewIAMRole)
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] = true;
                    HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] = null;
                }
                else
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] = false;
                    HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] = _pageUI.ServiceIAMRole;
                }

                string vpcId = null;
                var subnetIds = new List<string>();
                foreach(var subnet in this._pageUI.SelectedSubnets)
                {
                    vpcId = subnet.VpcId;
                    subnetIds.Add(subnet.SubnetId);
                }
                HostingWizard[PublishContainerToAWSWizardProperties.LaunchSubnets] = subnetIds.ToArray();

                HostingWizard[PublishContainerToAWSWizardProperties.VpcId] = vpcId;

                var groupIds = new List<string>();
                foreach(var group in this._pageUI.SelectedSecurityGroups)
                {
                    groupIds.Add(group.GroupId);
                }
                HostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] = groupIds.ToArray();

                HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] = this._pageUI.ServiceIAMRole;
            }

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        private Cluster FetchExistingCluster(string cluster)
        {
            using (var client = CreateECSClient(this.HostingWizard))
            {
                var response = client.DescribeClusters(new DescribeClustersRequest
                {
                    Clusters = new List<string> { cluster }
                });

                return response.Clusters[0];
            }
        }
    }
}
