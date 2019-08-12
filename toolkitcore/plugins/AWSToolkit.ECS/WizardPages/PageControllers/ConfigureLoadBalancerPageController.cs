using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.EC2.Model;
using Amazon.ECS.Model;
using System;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class ConfigureLoadBalancerPageController : IAWSWizardPageController
    {
        private ConfigureLoadBalancerPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription => "Using an Application Load Balancer allows multiple instances of the application be accessible through a single URL endpoint.";

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle => "Application Load Balancer Configuration";

        public string ShortPageTitle => null;

        public void ResetPage()
        {
            this._pageUI = null;
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        public string Service
        {
            get;
            private set;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // Role could have already been set on on the cluster page if launched as fargate
            this._pageUI.EnableServiceIAMRole = !string.Equals(this.HostingWizard[PublishContainerToAWSWizardProperties.LaunchType] as string, Amazon.ECS.LaunchType.FARGATE.Value, StringComparison.OrdinalIgnoreCase);
            if(HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] is Amazon.IdentityManagement.Model.Role)
            {
                this._pageUI.ServiceIAMRole = HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] as Amazon.IdentityManagement.Model.Role;
            }
            else if(HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] is bool && (bool)HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole])
            {
                this._pageUI.SetCreateNewIAMRole();
            }

            var service = HostingWizard[PublishContainerToAWSWizardProperties.Service] as string;
            if (!string.Equals(service, this.Service))
            {
                this.Service = service;
                this._pageUI.InitializeForNewService();
            }

            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ConfigureLoadBalancerPage(this);
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
                if (mode == Constants.DeployMode.DeployService &&
                    (HostingWizard[PublishContainerToAWSWizardProperties.CreateNewService] is bool) &&
                    ((bool)HostingWizard[PublishContainerToAWSWizardProperties.CreateNewService]))
                {
                    return true;
                }
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

            HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB] = this._pageUI.ShouldConfigureELB;

            if (!this.HostingWizard.IsFargateLaunch())
            {
                if (this._pageUI.CreateNewIAMRole)
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] = true;
                    HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] = null;
                }
                else
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] = false;

                    if (this._pageUI.ServiceIAMRole != null && !this._pageUI.ServiceIAMRole.Path.Contains("aws-service-role/ecs.amazonaws.com"))
                    {
                        HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] = _pageUI.ServiceIAMRole.RoleName;
                    }
                }
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] = null;
            }

            if (this._pageUI.CreateNewLoadBalancer)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewLoadBalancer] = true;
                HostingWizard[PublishContainerToAWSWizardProperties.LoadBalancer] = _pageUI.NewLoadBalancerName;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewLoadBalancer] = false;
                HostingWizard[PublishContainerToAWSWizardProperties.LoadBalancer] = _pageUI.LoadBalancerArn;
            }

            if (this._pageUI.CreateNewListenerPort)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort] = true;
                HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort] = this._pageUI.NewListenerPort.GetValueOrDefault();
                HostingWizard[PublishContainerToAWSWizardProperties.ListenerArn] = null;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort] = false;
                HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort] = null;
                HostingWizard[PublishContainerToAWSWizardProperties.ListenerArn] = this._pageUI.ListenerArn;
            }

            if (this._pageUI.CreateNewTargetGroup)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup] = true;
                HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] = this._pageUI.NewTargetGroupName;
                HostingWizard[PublishContainerToAWSWizardProperties.NewPathPattern] = this._pageUI.PathPattern;

                try
                {
                    HostingWizard[PublishContainerToAWSWizardProperties.VpcId] = DetermineClusterVPC();
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Failed to determine VPC for cluster: " + e.Message);
                    return false;
                }
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup] = false;
                HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] = _pageUI.TargetGroupArn;
                HostingWizard[PublishContainerToAWSWizardProperties.NewPathPattern] = null;
            }

            if (_pageUI.IsHealthCheckPathChanged)
            {
                HostingWizard[PublishContainerToAWSWizardProperties.HealthCheckPath] = _pageUI.HealthCheckPath;
            }
            else
            {
                HostingWizard[PublishContainerToAWSWizardProperties.HealthCheckPath] = null;
            }

            return true;
        }

        private string DetermineClusterVPC()
        {
            if (string.Equals(this.HostingWizard[PublishContainerToAWSWizardProperties.LaunchType] as string, Amazon.ECS.LaunchType.FARGATE.Value, StringComparison.OrdinalIgnoreCase))
            {
                return this.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string;
            }
            else
            {
                using (var ec2Client = ECSWizardUtils.CreateEC2Client(this.HostingWizard))
                using (var ecsClient = ECSWizardUtils.CreateECSClient(this.HostingWizard))
                {
                    var cluster = HostingWizard[PublishContainerToAWSWizardProperties.ExistingCluster] as Amazon.ECS.Model.Cluster;
                    var containerInstanceArns = ecsClient.ListContainerInstances(new ListContainerInstancesRequest
                    {
                        Cluster = cluster.ClusterName
                    }).ContainerInstanceArns;

                    var containerInstances = ecsClient.DescribeContainerInstances(new DescribeContainerInstancesRequest
                    {
                        Cluster = cluster.ClusterName as string,
                        ContainerInstances = containerInstanceArns
                    }).ContainerInstances;

                    var describeIntanceRequest = new DescribeInstancesRequest();
                    containerInstances.ForEach(x => describeIntanceRequest.InstanceIds.Add(x.Ec2InstanceId));

                    string vpcId = null;
                    var reservations = ec2Client.DescribeInstances(describeIntanceRequest).Reservations;
                    foreach (var reservation in reservations)
                    {
                        foreach (var instance in reservation.Instances)
                        {
                            if (!string.IsNullOrWhiteSpace(instance.VpcId))
                            {
                                vpcId = instance.VpcId;
                                break;
                            }
                        }
                    }

                    if (vpcId == null)
                    {
                        throw new Exception("There are no EC2 instances in the cluster currently running with a VPC");
                    }

                    return vpcId;
                }
            }
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
