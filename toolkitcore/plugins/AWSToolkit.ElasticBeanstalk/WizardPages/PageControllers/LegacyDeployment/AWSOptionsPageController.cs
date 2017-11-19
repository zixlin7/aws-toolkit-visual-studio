using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;

using Amazon.ElasticBeanstalk.Model;

using Amazon.AWSToolkit.Account;
using System.Threading;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.IdentityManagement.Model;
using AWSDeployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    internal class AWSOptionsPageController : IAWSWizardPageController
    {
        AWSOptionsPage _pageUI = null;
        string _lastSeenAccount = string.Empty;
        List<string> _windowsSolutionStacks = null;
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

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get;  set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "AWS Options"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Set Amazon EC2 options for the deployed application."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (IsWizardInBeanstalkMode)
                return ((bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv]);
            else
                return false;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new AWSOptionsPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);

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
                // if we last populated for this a/c, don't bother again
                if (string.Compare(selectedAccount.AccountDisplayName, _lastSeenAccount, StringComparison.CurrentCulture) != 0)
                {
                    _lastSeenAccount = selectedAccount.AccountDisplayName;

                    var region = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;

                    LoadSolutionStacks(selectedAccount, region);
                    LoadExistingKeyPairs(selectedAccount, region);
                    LoadInstanceProfiles(selectedAccount, region);
                }
                else
                {
                    // may be same user, but might have gone back and changed template/environment type
                    var stacks = FilterStacksForSingleInstanceEnvironment(_windowsSolutionStacks);
                    string defaultStackName = ToolkitAMIManifest.Instance.QueryDefaultWebDeploymentContainer(ToolkitAMIManifest.HostService.ElasticBeanstalk);
                    _pageUI.SetSolutionStacks(stacks, defaultStackName);
                }
            }

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            if (!IsWizardInBeanstalkMode)
                return true;

            // if we know the user is creating a new env, they have to access this page to get a solution
            // stack. They can only bypass options for known re-use of an environment.
            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv))
            {
                if ((bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv])
                {
                    if ((_windowsSolutionStacks != null && _windowsSolutionStacks.Count == 1)
                            || (_pageUI != null && !string.IsNullOrEmpty(_pageUI.SolutionStack)))
                        return true;
                }
                else
                    return true;
            }

            return false;
        }

        public void TestForwardTransitionEnablement()
        {
            bool enableNext = _pageUI.SolutionStack != null
                                && _pageUI.SelectedInstanceType != null; // optional field, but null means service failure on population

            // unless the user has requested a custom ami, a keypair is mandatory
            // - restriction lifted now that EC2 team has fixed bug in ec2config that caused no-keypair
            //   deployments to take excessive time
            //if (enableNext && string.IsNullOrEmpty(_pageUI.CustomAMIID))
            //    enableNext = _pageUI.HasKeyPairSelection;

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enableNext);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        #endregion

        bool IsWizardInBeanstalkMode
        {
            get
            {
                var service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.BeanstalkServiceName;
            }
        }

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "solutionstack")
                QuerySolutionStackInstanceTypes(string.Empty);

            // need to have this set so downstream pages can eval Finish enablement
            if (e.PropertyName == "vpc")
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = _pageUI.LaunchIntoVPC;

            TestForwardTransitionEnablement();
        }

        void StorePageData()
        {
            if (!IsWizardInBeanstalkMode)
                return;

            Amazon.AWSToolkit.EC2.InstanceType instanceType;
            // if user short-circuiting wizard, our ui may not have been shown
            if (_pageUI != null)
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] = _pageUI.SolutionStack;
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

                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName] = _pageUI.SelectedInstanceProfile;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = _pageUI.LaunchIntoVPC;
            }
            else
            {
                if ((bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv])
                {
                    HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] = _windowsSolutionStacks[0];
                    if (!BeanstalkDeploymentEngine.IsLegacyContainer(_windowsSolutionStacks[0]))
                        HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName] = BeanstalkParameters.DefaultRoleName;
                }

                // "optional defaults", if that makes any sense!
                instanceType = EC2ServiceMeta.FindById("t1.micro");
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] = instanceType.Id;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] = instanceType.Name;

                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair] = false;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] = string.Empty;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC] = false;
            }
        }

        void LoadSolutionStacks(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
        {
            try
            {
                var beanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(selectedAccount, region);
                var response = beanstalkClient.ListAvailableSolutionStacks(new ListAvailableSolutionStacksRequest());

                bool isSingleInstanceEnvironment = DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard);
                _windowsSolutionStacks = new List<string>();
                foreach (var stack in response.SolutionStacks.Where(stack => stack.Contains(" Windows ")))
                {
                    _windowsSolutionStacks.Add(stack);
                }

                var stacks = FilterStacksForSingleInstanceEnvironment(_windowsSolutionStacks);
                string defaultStackName = ToolkitAMIManifest.Instance.QueryDefaultWebDeploymentContainer(ToolkitAMIManifest.HostService.ElasticBeanstalk);
                _pageUI.SetSolutionStacks(stacks, defaultStackName);
            }
            catch (Exception e)
            {
                HostingWizard.Logger.Error(GetType().FullName + ", exception in LoadSolutionStacks", e);
            }
        }

        // if the user requested a single-instance environment, filter out legacy non-cloudformation stacks
        IEnumerable<string> FilterStacksForSingleInstanceEnvironment(IEnumerable<string> allWindowsSolutionStacks)
        {
            var stacks = new List<string>();
            bool singleInstanceEnvSelected = DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard);
            foreach (var stack in allWindowsSolutionStacks)
            {
                if (stack.Contains("legacy") && singleInstanceEnvSelected)
                    continue;

                stacks.Add(stack);
            }

            return stacks;
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

        void LoadInstanceProfiles(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
        {
            Interlocked.Increment(ref _workersActive);
            new QueryInstanceProfilesWorker(selectedAccount,
                                            region,
                                            HostingWizard.Logger,
                                            new QueryInstanceProfilesWorker.DataAvailableCallback(OnInstanceProfilesAvailable));
            TestForwardTransitionEnablement();
        }

        void OnInstanceProfilesAvailable(IEnumerable<InstanceProfile> data)
        {
            _pageUI.SetInstanceProfiles(data, string.Empty);
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

        private void QuerySolutionStackInstanceTypes(string solutionStack)
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
                                                 new SolutionStackInstanceSizesWorker.DataAvailableCallback(OnInstanceSizesAvailable));

            _pageUI.SetInstanceTypes(null);
        }

        private void OnInstanceSizesAvailable(IEnumerable<string> instanceSizes)
        {
            _pageUI.SetInstanceTypes(instanceSizes);
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

    }
}
