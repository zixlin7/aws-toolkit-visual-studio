using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.ElasticBeanstalk.Model;
using Amazon.IdentityManagement;
using log4net;
using AWSOptionsPage = Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment.AWSOptionsPage;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.Deployment
{
    internal class AWSOptionsPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSOptionsPageController));

        private AWSOptionsPage _pageUI;
        private IAmazonIdentityManagementService _iamClient;
        private string _lastSeenAccount = string.Empty;
        private string _lastSeenRegion = string.Empty;
        private List<string> _windowsSolutionStacks;

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

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return DeploymentWizardPageGroups.AWSOptionsGroup; }
        }

        public string PageTitle
        {
            get { return "AWS"; }
        }

        public string ShortPageTitle
        {
            get { return null; /* use the group title here, allowing VPC subpage */ }
        }

        public string PageDescription
        {
            get { return "Set Amazon EC2 and other AWS-related options for the deployed application."; }
        }

        public IAmazonIdentityManagementService IAMClient
        {
            get { return this._iamClient;}
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
                var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                var selectedRegion = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;

                // if we last populated for this a/c, don't bother again
                if (!string.Equals(selectedAccount.AccountDisplayName, _lastSeenAccount, StringComparison.CurrentCulture) 
                        || !string.Equals(selectedRegion.SystemName, _lastSeenRegion, StringComparison.CurrentCulture))
                {
                    _lastSeenAccount = selectedAccount.AccountDisplayName;
                    _lastSeenRegion = selectedRegion.SystemName;

                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode))
                        _pageUI.VpcOnlyMode = (bool)HostingWizard[DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode];

                    CheckForDefaultVPC(selectedAccount, selectedRegion);
                    LoadSolutionStacks(selectedAccount, selectedRegion);
                    LoadExistingKeyPairs(selectedAccount, selectedRegion);
                    LoadRDSGroupAndInstanceData(selectedAccount, selectedRegion);

                    this._iamClient = DeploymentWizardHelper.GetIAMClient(selectedAccount, selectedRegion);
                    this._pageUI.InitializeIAM(this._iamClient);
                }
                else
                {
                    // may be same user, but might have gone back and changed template/environment type
                    var stacks = FilterStacksForSingleInstanceEnvironment(_windowsSolutionStacks);
                    var defaultStackName =
                        ToolkitAMIManifest.Instance.QueryDefaultWebDeploymentContainer(
                            ToolkitAMIManifest.HostService.ElasticBeanstalk);
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

                string keypairName;
                bool createNew;
                _pageUI.QueryKeyPairSelection(out keypairName, out createNew);
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] = keypairName;
                if (!string.IsNullOrEmpty(keypairName))
                    HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair] = createNew;

                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType,
                                          _pageUI.SingleInstanceEnvironment
                                              ? BeanstalkConstants.EnvType_SingleInstance
                                              : BeanstalkConstants.EnvType_LoadBalanced);

                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnableRollingDeployments] = 
                    _pageUI.EnableRollingDeployments && !_pageUI.SingleInstanceEnvironment;

                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = _pageUI.UseNonDefaultVpc;

                // if we are using the default vpc in a vpc-by-default environment, set up the default security group as if the
                // user had gone to the vpc page
                if (!_pageUI.UseNonDefaultVpc)
                    QueryAndSetDefaultVpcProperties();

                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName] = _pageUI.SelectedInstanceProfile;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_PolicyTemplates] = _pageUI.SelectedPolicyTemplates;

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
                    HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] = _windowsSolutionStacks[0];
                }

                // if the user has selected to launch into a vpc, should we switch this default instance type to a t2?
                instanceType = EC2ServiceMeta.FindById("t1.micro");
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] = instanceType.Id;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] = instanceType.Name;

                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair] = false;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] = string.Empty;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = false;
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType, BeanstalkConstants.EnvType_SingleInstance);
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
                var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                var selectedRegion = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;

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

        void LoadSolutionStacks(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
        {
            try
            {
                var beanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(selectedAccount, region);
                var response = beanstalkClient.ListAvailableSolutionStacks(new ListAvailableSolutionStacksRequest());

                _windowsSolutionStacks = new List<string>();
                foreach (var stack in response.SolutionStacks.Where(stack => stack.Contains(" Windows ")))
                {
                    _windowsSolutionStacks.Add(stack);
                }

                var stacks = FilterStacksForSingleInstanceEnvironment(_windowsSolutionStacks);
                var defaultStackName = ToolkitAMIManifest.Instance.QueryDefaultWebDeploymentContainer(ToolkitAMIManifest.HostService.ElasticBeanstalk);
                _pageUI.SetSolutionStacks(stacks, defaultStackName);
            }
            catch (Exception e)
            {
                HostingWizard.Logger.Error(GetType().FullName + ", exception in LoadSolutionStacks", e);
            }
        }

        void CheckForDefaultVPC(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
        {
            var ec2Client = DeploymentWizardHelper.GetEC2Client(selectedAccount, region);

            string defaultVpcId = null;
            int vpcCount = 0;

            var request = new Amazon.EC2.Model.DescribeVpcsRequest();
            try
            {
                ec2Client.BeginDescribeVpcs(request, ar =>
                    {
                        try
                        {
                            var response = ec2Client.EndDescribeVpcs(ar);
                            foreach (var v in response.Vpcs.Where(v => v.IsDefault))
                            {
                                defaultVpcId = v.VpcId;
                                break;
                            }                            
                            vpcCount = response.Vpcs.Count;
                        }
                        catch (Exception e)
                        {
                            HostingWizard.Logger.Error(GetType().FullName + ", exception in callback for getting default VPC", e);
                        }

                        // log this in case we need it later during deployment or for 
                        // review confirmation
                        HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_DefaultVpcId] = defaultVpcId;
                        this._pageUI.SetVpcAvailability(defaultVpcId, vpcCount);
                    }, null);
            }
            catch(Exception e)
            {
                HostingWizard.Logger.Error(GetType().FullName + ", exception for getting default VPC", e);
                this._pageUI.SetVpcAvailability(defaultVpcId, vpcCount);
            }
        }

        // if the user requested a single-instance environment, filter out legacy non-cloudformation stacks
        IEnumerable<string> FilterStacksForSingleInstanceEnvironment(IEnumerable<string> allWindowsSolutionStacks)
        {
            var singleInstanceEnvSelected = _pageUI.SingleInstanceEnvironment;
            return allWindowsSolutionStacks.Where(stack => !stack.Contains("legacy") || !singleInstanceEnvSelected).ToList();
        }

        void LoadExistingKeyPairs(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
        {
            Interlocked.Increment(ref _workersActive);
            new QueryKeyPairNamesWorker(selectedAccount,
                                        region.SystemName,
                                        DeploymentWizardHelper.GetEC2Client(selectedAccount, region),
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

        void LoadRDSGroupAndInstanceData(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
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
            var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            new SolutionStackInstanceSizesWorker(selectedAccount,
                                                 HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                                   as RegionEndPointsManager.RegionEndPoints,
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
