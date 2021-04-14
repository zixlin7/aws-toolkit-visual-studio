﻿using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Amazon.ECS;
using Amazon.ECS.Model;

using static Amazon.AWSToolkit.ECS.WizardPages.ECSWizardUtils;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class ECSClusterPageController : IAWSWizardPageController
    {
        private ECSClusterPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription => "Choose how to provide compute capacity to your application.";

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle => "Launch Configuration";

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
            this._pageUI.PageActivated();
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

        public bool IsFargateSupported
        {
            get
            {
                if (!HostingWizard.IsPropertySet(PublishContainerToAWSWizardProperties.IsFargateSupported))
                    return false;

                return (bool)HostingWizard[PublishContainerToAWSWizardProperties.IsFargateSupported];
            }
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

        bool _lastCreateNewCluster;
        string _lastExistingClusterName;
        Amazon.ECS.LaunchType _lastLaunchType;

        bool StorePageData()
        {
            if (_pageUI == null)
                return false;

            bool resetForwardPages = false;

            if(_lastLaunchType != this._pageUI.LaunchType)
            {
                resetForwardPages = true;
            }
            _lastLaunchType = this._pageUI.LaunchType;


            if (this._pageUI.CreateNewCluster)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewCluster] = this._pageUI.CreateNewCluster;
                HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] = this._pageUI.NewClusterName;

                if (!_lastCreateNewCluster)
                {
                    resetForwardPages = true;
                }
                _lastCreateNewCluster = true;
            }
            else
            {
                if (_lastCreateNewCluster)
                {
                    resetForwardPages = true;
                }
                _lastCreateNewCluster = false;

                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewCluster] = this._pageUI.CreateNewCluster;
                try
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.ExistingCluster] = FetchExistingCluster(this._pageUI.Cluster);
                }
                catch(Exception e)
                {
                    this.HostingWizard.SetPageError("Error describing existing cluster: " + e.Message);
                    return false;
                }
                HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] = this._pageUI.Cluster;

                if(!string.Equals(this._pageUI.Cluster, _lastExistingClusterName))
                {
                    resetForwardPages = true;
                }
                _lastExistingClusterName = this._pageUI.Cluster;
            }

            HostingWizard[PublishContainerToAWSWizardProperties.LaunchType] = this._pageUI.LaunchType.Value;
            if (this._pageUI.LaunchType == Amazon.ECS.LaunchType.FARGATE)
            {
                string vpcId = null;
                var subnetIds = new List<string>();
                foreach (var subnet in this._pageUI.SelectedSubnets)
                {
                    vpcId = subnet.VpcId;
                    subnetIds.Add(subnet.SubnetId);
                }
                HostingWizard[PublishContainerToAWSWizardProperties.LaunchSubnets] = subnetIds.ToArray();

                HostingWizard[PublishContainerToAWSWizardProperties.VpcId] = vpcId;

                if (this._pageUI.CreateNewSecurityGroup)
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.CreateNewSecurityGroup] = true;
                    HostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] = null;
                }
                else
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.CreateNewSecurityGroup] = false;
                    HostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] = new string[] { this._pageUI.SecurityGroup };
                }

                HostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskCPU] = this._pageUI.TaskCPU;
                HostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskMemory] = this._pageUI.TaskMemory;
                HostingWizard[PublishContainerToAWSWizardProperties.AssignPublicIpAddress] = this._pageUI.AssignPublicIpAddress;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.VpcId] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskCPU] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskMemory] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.AssignPublicIpAddress] = false;
            }

            if(resetForwardPages)
            {
                this.HostingWizard.NotifyForwardPagesReset(this);
            }

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        private Cluster FetchExistingCluster(string cluster)
        {
            using (var client = CreateServiceClient<AmazonECSClient>(this.HostingWizard))
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
