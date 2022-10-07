using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.EC2;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancingV2;

using log4net;

using AWSOptionsPage = Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment.AWSOptionsPage;
using InstanceType = Amazon.AWSToolkit.EC2.InstanceType;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    public class AWSOptionsPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSOptionsPageController));

        // HACK: We don't have a nice UX for Solution Stacks, because they are a flat string representing multiple variables
        // TODO : Longer term, switch away from SolutionStack, use PlatformBranches/PlatformSummaries, and 
        // provide a UX with multiple dropdowns, like the web console.
        private static readonly Dictionary<string, int> SolutionStackOrder = new Dictionary<string, int>()
        {
            {BeanstalkConstants.SolutionStackNames.Systems.AmazonLinux, 0},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2019Core, 1},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2019, 2},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2016Core, 3},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2016, 4},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2012R2Core, 5},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2012R2, 6},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2012Core, 7},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2012, 8},
            {BeanstalkConstants.SolutionStackNames.Systems.WindowsServer2008, 9},
        };

        protected AWSOptionsPage _pageUI;
        private string _lastSeenAccount = string.Empty;
        private string _lastSeenRegion = string.Empty;
        private readonly List<string> _availableSolutionStacks = new List<string>();

        readonly object _syncLock = new object();

        int _workersActive = 0;

        public bool WorkersActive
        {
            get
            {
                bool ret;
                lock (_syncLock)
                    ret = _workersActive > 0;
                return ret;
            }
        }

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => DeploymentWizardPageGroups.AWSOptionsGroup;

        public string PageTitle => "AWS";

        public string ShortPageTitle => null;

        public string PageDescription => "Set Amazon EC2 and other AWS-related options for the deployed application.";

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {            
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
            {
                var isRedeploying = (bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
                if (isRedeploying)
                    return false;
            }

            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new AWSOptionsPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_CustomAMIID))
                    _pageUI.CustomAMIID = HostingWizard[DeploymentWizardProperties.SeedData.propkey_CustomAMIID] as string;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                var selectedAccount = HostingWizard.GetSelectedAccount();
                var selectedRegion = HostingWizard.GetSelectedRegion();

                // if we last populated for this a/c, don't bother again
                if (!string.Equals(selectedAccount.DisplayName, _lastSeenAccount, StringComparison.CurrentCulture) 
                        || !string.Equals(selectedRegion.Id, _lastSeenRegion, StringComparison.CurrentCulture))
                {
                    _lastSeenAccount = selectedAccount.DisplayName;
                    _lastSeenRegion = selectedRegion.Id;

                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode))
                        _pageUI.VpcOnlyMode = (bool)HostingWizard[DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode];

                    CheckForDefaultVPC(selectedAccount, selectedRegion);
                    LoadSolutionStacks(selectedAccount, selectedRegion);
                    LoadExistingKeyPairs(selectedAccount, selectedRegion);
                    LoadRDSGroupAndInstanceData(selectedAccount, selectedRegion);
                }
                else
                {
                    // may be same user, but might have gone back and changed template/environment type
                    var stacks = GetSolutionStacks().ToList();

                    var defaultStackName = DeploymentWizardHelper.PickDefaultSolutionStack(stacks);
                    
                    _pageUI.SetSolutionStacks(stacks, defaultStackName);
                }
            }

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if(navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                if(this._pageUI.SelectedInstanceType.RequiresVPC)
                {
                    if (!this._pageUI.HasDefaultVpc && !this._pageUI.UseNonDefaultVpc)
                    {
                        ToolkitFactory.Instance.ShellProvider.ShowError("VPC Required", "A VPC is required to launch instances of type " + this._pageUI.SelectedInstanceType.Id);
                        return false;
                    }
                }
            }

            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            var enableNext = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enableNext);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false; // not an optional page

                return _pageUI.SolutionStack != null
                                    && _pageUI.SelectedInstanceType != null; // optional field, but null means service failure on population

            }
        }

        void StorePageData()
        {
            InstanceType instanceType;
            // if user short-circuiting wizard, our ui may not have been shown
            if (_pageUI != null)
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] = _pageUI.SolutionStack;
                if (!string.IsNullOrEmpty(_pageUI.CustomAMIID))
                    HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] = _pageUI.CustomAMIID.Trim();

                instanceType = _pageUI.SelectedInstanceType;
                if (instanceType != null)
                {
                    HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] = instanceType.Id;
                    HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] = instanceType.Name;
                }

                HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack] = !Amazon.ElasticBeanstalk.Tools.EBUtilities.IsSolutionStackWindows(_pageUI.SolutionStack);

                string keypairName;
                bool createNew;
                _pageUI.QueryKeyPairSelection(out keypairName, out createNew);
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] = keypairName;
                if (!string.IsNullOrEmpty(keypairName))
                {
                    HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair] = createNew;
                }

                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType,
                                          _pageUI.SingleInstanceEnvironment
                                              ? BeanstalkConstants.EnvType_SingleInstance
                                              : BeanstalkConstants.EnvType_LoadBalanced);

                if (!_pageUI.SingleInstanceEnvironment)
                {
                    if (!"classic".Equals(_pageUI.SelectedLoadBalancerType))
                    {
                        HostingWizard.SetProperty(
                            BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_LoadBalancerType,
                            _pageUI.SelectedLoadBalancerType == null
                                ? LoadBalancerTypeEnum.Application.Value
                                : _pageUI.SelectedLoadBalancerType);
                    }
                }
                else
                {
                    HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_LoadBalancerType, null);
                }

                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnableRollingDeployments] = 
                    _pageUI.EnableRollingDeployments && !_pageUI.SingleInstanceEnvironment;

                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = _pageUI.UseNonDefaultVpc;

                // if we are using the default vpc in a vpc-by-default environment, set up the default security group as if the
                // user had gone to the vpc page
                if (!_pageUI.UseNonDefaultVpc)
                    QueryAndSetDefaultVpcProperties();

                var dbSecurityGroups = _pageUI.SelectedSecurityGroups;
                if (dbSecurityGroups.Count > 0)
                {
                    var rdsGroups = new List<string>();
                    var vpcGroups = new List<string>();
                    foreach (var g in dbSecurityGroups)
                    {
                        if (g.IsVPCGroup)
                            vpcGroups.Add(g.Id);
                        else
                            rdsGroups.Add(g.Name);
                    }
                    HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] = rdsGroups;
                    HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCSecurityGroups] = vpcGroups;

                    // we use this extra data to support vpc-based rds instances, where we need (and only want) to open the specific 
                    // port the db instance is listening on
                    HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCGroupsAndDBInstances] = _pageUI.VPCGroupsAndReferencingDBInstances;
                }
                else
                {
                    HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] = null;
                    HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCSecurityGroups] = null;
                    HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCGroupsAndDBInstances] = null;
                }
            }
            else
            {
                if ((bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv])
                {
                    HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] =
                        _availableSolutionStacks.First(SolutionStackUtils.SolutionStackIsWindows);
                }

                // if the user has selected to launch into a vpc, should we switch this default instance type to a t2?
                instanceType = EC2ServiceMeta.FindById("t1.micro");
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] = instanceType.Id;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] = instanceType.Name;

                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair] = false;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] = string.Empty;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = false;
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType, BeanstalkConstants.EnvType_SingleInstance);
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_LoadBalancerType, null);
            }
        }

        // if a default vpc is available (for vpc-by-default users), probe for the vpc settings we would have set up
        // on the vpc page of the wizarc (which we are not going to visit as the user hasn't elected to use a custom vpc)
        void QueryAndSetDefaultVpcProperties()
        {
            if (!HostingWizard.IsPropertySet(DeploymentWizardProperties.AWSOptions.propkey_DefaultVpcId))
                return;

            var defaultVpcId = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_DefaultVpcId] as string;
            if (string.IsNullOrEmpty(defaultVpcId))
                return;

            try
            {
                var selectedAccount = HostingWizard.GetSelectedAccount();
                var selectedRegion = HostingWizard.GetSelectedRegion();

                var vpcProps = QueryVPCPropertiesWorker.QueryVPCProperties(selectedAccount, selectedRegion, defaultVpcId, LOGGER);
                if (vpcProps != null)
                {
                    HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCSecurityGroup] = vpcProps.DefaultSecurityGroupId;
                    HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceSubnet] = vpcProps.Subnets.First().Value.SubnetId;
                }
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Failed to determine default security group and subnet to use for vpc {0}, error {1}", defaultVpcId, e.Message);
            }
        }

        void OnPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "solutionstack")
                QuerySolutionStackInstanceTypes(string.Empty);

            // need to have this set so downstream pages can eval Finish enablement
            if (e.PropertyName == "vpc")
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = _pageUI.UseNonDefaultVpc;

            TestForwardTransitionEnablement();
        }

        void LoadSolutionStacks(AccountViewModel selectedAccount, ToolkitRegion region)
        {
            try
            {
                var beanstalkClient = selectedAccount.CreateServiceClient<AmazonElasticBeanstalkClient>(region);
                var response = beanstalkClient.ListAvailableSolutionStacks(new ListAvailableSolutionStacksRequest());

                _availableSolutionStacks.Clear();
                _availableSolutionStacks.AddRange(response.SolutionStacks);

                var stacks = GetSolutionStacks().ToList();
                var defaultStackName = DeploymentWizardHelper.PickDefaultSolutionStack(stacks);
                _pageUI.SetSolutionStacks(stacks, defaultStackName);
            }
            catch (Exception e)
            {
                HostingWizard.Logger.Error(GetType().FullName + ", exception in LoadSolutionStacks", e);
            }
        }

        void CheckForDefaultVPC(AccountViewModel selectedAccount, ToolkitRegion region)
        {
            var ec2Client = selectedAccount.CreateServiceClient<AmazonEC2Client>(region);

            string defaultVpcId = null;
            int vpcCount = 0;

            var request = new Amazon.EC2.Model.DescribeVpcsRequest();
            try
            {
                ec2Client.DescribeVpcsAsync(request).ContinueWith(task =>
                {
                    if (task.Exception == null)
                    {
                        var response = task.Result;
                        foreach (var v in response.Vpcs.Where(v => v.IsDefault))
                        {
                            defaultVpcId = v.VpcId;
                            break;
                        }
                        vpcCount = response.Vpcs.Count;
                    }
                    else
                    {
                        HostingWizard.Logger.Error("CheckForDefaultVPC: exception from DescribeVpcsAsync.", task.Exception);
                    }

                    HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_DefaultVpcId] = defaultVpcId;
                    this._pageUI.SetVpcAvailability(defaultVpcId, vpcCount);
                });
            }
            catch(Exception e)
            {
                HostingWizard.Logger.Error(GetType().FullName + ", exception for getting default VPC", e);
                this._pageUI.SetVpcAvailability(defaultVpcId, vpcCount);
            }
        }

        /// <summary>
        /// Returns the SolutionStacks that are relevant to the current Beanstalk settings
        /// </summary>
        public IEnumerable<string> GetSolutionStacks()
        {
            IEnumerable<string> stacks = _availableSolutionStacks;

            // if the user requested a single-instance environment, filter out legacy non-cloudformation stacks
            if (_pageUI.SingleInstanceEnvironment)
            {
                stacks = stacks.Where(stack => !SolutionStackUtils.SolutionStackIsLegacy(stack));
            }

            if (Project.IsStandardWebProject(HostingWizard))
            {
                stacks = stacks.Where(SolutionStackUtils.SolutionStackSupportsDotNetFramework);
            }
            else if (Project.IsNetCoreWebProject(HostingWizard))
            {
                stacks = stacks.Where(SolutionStackUtils.SolutionStackSupportsDotNetCore);
            }

            // HACK: We don't have a nice UX for Solution Stacks, because they are a flat string representing multiple variables
            // Group them by OS of interest, then by reverse version, then (if necessary) by reverse order.
            // The intention is to show the newest versions higher in the list for each OS/image.
            // TODO : Longer term, switch away from SolutionStack, use PlatformBranches/PlatformSummaries, and 
            // provide a UX with multiple dropdowns, like the web console.
            stacks = stacks.OrderBy(stack =>
            {
                var stackOrderPreference = SolutionStackOrder.FirstOrDefault(entry => stack.Contains(entry.Key));
                return stackOrderPreference.Equals(default(KeyValuePair<string, int>))
                    ? SolutionStackOrder.Count
                    : stackOrderPreference.Value;
            }).ThenByDescending(stack =>
            {
                // We are now grouped by OS image (eg: "64bit Windows Server 2019").
                // If we can parse a version, order them from highest to lowest
                if (SolutionStackUtils.TryGetVersion(stack, out Version version))
                {
                    return version;
                }

                // If we can't parse a version, rank this "last", and the alphabetical sort (next) can handle the ordering.
                return new Version(int.MaxValue, int.MaxValue, int.MaxValue);
            }).ThenByDescending(stack => stack);

            return stacks;
        }

        void LoadExistingKeyPairs(AccountViewModel selectedAccount, ToolkitRegion region)
        {
            Interlocked.Increment(ref _workersActive);
            new QueryKeyPairNamesWorker(selectedAccount,
                                        region.Id,
                                        selectedAccount.CreateServiceClient<AmazonEC2Client>(region),
                                        HostingWizard.Logger,
                                        new QueryKeyPairNamesWorker.DataAvailableCallback(OnKeyPairsAvailable));
            TestForwardTransitionEnablement();
        }

        void OnKeyPairsAvailable(ICollection<string> keypairNames, ICollection<string> keyPairsStoredInToolkit)
        {
            _pageUI.SetExistingKeyPairs(keypairNames, keyPairsStoredInToolkit, string.Empty);
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

        void LoadRDSGroupAndInstanceData(AccountViewModel selectedAccount, ToolkitRegion region)
        {
            Interlocked.Increment(ref _workersActive);
            new QueryRDSGroupsAndInstancesWorker(selectedAccount, region, LOGGER, OnRDSGroupsAndInstancesAvailable);
        }

        void OnRDSGroupsAndInstancesAvailable(IEnumerable<SelectableGroup<SecurityGroupInfo>> data)
        {
            _pageUI.SetSecurityGroupsAndInstances(data);
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

        void QuerySolutionStackInstanceTypes(string solutionStack)
        {
            Interlocked.Increment(ref _workersActive);
            new SolutionStackInstanceSizesWorker(HostingWizard.GetSelectedAccount(),
                                                 HostingWizard.GetSelectedRegion(),
                                                 string.IsNullOrEmpty(solutionStack)
                                                   ? _pageUI.SolutionStack
                                                   : solutionStack,
                                                 HostingWizard.Logger,
                                                 OnInstanceSizesAvailable);

            _pageUI.SetInstanceTypes(null);
        }

        void OnInstanceSizesAvailable(IEnumerable<string> instanceSizes)
        {
            _pageUI.SetInstanceTypes(instanceSizes);
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }
    }
}
