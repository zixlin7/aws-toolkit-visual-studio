using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    internal class VpcOptionsPageController : IAWSWizardPageController
    {
        private VpcOptionsPage _pageUI;
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

        public string PageTitle => "VPC Options";

        public string ShortPageTitle => "VPC";

        public string PageDescription => "Set Amazon VPC options for the deployed application.";

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

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC))
            {
                return (bool)HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC];
            }

            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new VpcOptionsPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                QueryVPCs();
                _pageUI.ConfigureForEnvironmentType();
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
            var vpcSelected = HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC)
                                && (bool)HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC];

            if (vpcSelected)
            {
                if (_pageUI == null)
                    return false;
                
                if (!IsSingleInstanceEnvironmentType)
                    return this._pageUI.SelectedVPCId != null
                            && this._pageUI.SelectedVPCSecurityGroupId != null
                            && this._pageUI.SelectedInstanceSubnetId != null
                            && this._pageUI.SelectedELBSubnetId != null;

                return this._pageUI.SelectedVPCId != null
                            && this._pageUI.SelectedVPCSecurityGroupId != null
                            && this._pageUI.SelectedInstanceSubnetId != null;
            }

            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            bool enableNext;
            if (!IsSingleInstanceEnvironmentType)
                enableNext = this._pageUI.SelectedVPCId != null
                                    && this._pageUI.SelectedVPCSecurityGroupId != null
                                    && this._pageUI.SelectedInstanceSubnetId != null
                                    && this._pageUI.SelectedELBSubnetId != null;
            else
                enableNext = this._pageUI.SelectedVPCId != null
                                    && this._pageUI.SelectedVPCSecurityGroupId != null
                                    && this._pageUI.SelectedInstanceSubnetId != null;

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enableNext);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        internal bool IsSingleInstanceEnvironmentType
        {
            get
            {
                if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType))
                    return ((string)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType])
                            .Equals(BeanstalkConstants.EnvType_SingleInstance);

                return false;
            }
        }
        void StorePageData()
        {
            if (_pageUI == null)
                return;

            HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCId] = _pageUI.SelectedVPCId;
            HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceSubnet] = _pageUI.SelectedInstanceSubnetId;
            HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCSecurityGroup] = _pageUI.SelectedVPCSecurityGroupId;
            if (!IsSingleInstanceEnvironmentType)
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBSubnet] = _pageUI.SelectedELBSubnetId;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBScheme] = _pageUI.SelectedELBScheme;
            }
            else
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBSubnet] = null;
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBScheme] = null;
            }
        }

        void OnPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "vpc")
                this.QueryVPCProperties(this._pageUI.SelectedVPCId);

            TestForwardTransitionEnablement();
        }

        private void QueryVPCs()
        {
            Interlocked.Increment(ref _workersActive);
            var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            new QueryExistingVPCsWorker(selectedAccount,
                                        HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                          as RegionEndPointsManager.RegionEndPoints,
                                        HostingWizard.Logger,
                                        OnVPCsAvailable);

            _pageUI.VPCs = null;
        }

        private void OnVPCsAvailable(IEnumerable<KeyValuePair<string, Amazon.EC2.Model.Vpc>> vpcs)
        {
            _pageUI.VPCs = vpcs;
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

        private void QueryVPCProperties(string vpcId)
        {
            Interlocked.Increment(ref _workersActive);
            var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            new QueryVPCPropertiesWorker(selectedAccount,
                                         HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                           as RegionEndPointsManager.RegionEndPoints,
                                         vpcId,
                                         HostingWizard.Logger,
                                         OnVPCPropertiesAvailable);

            _pageUI.VPCSecurityGroups = null;
            _pageUI.SetAvailableSubnets(null);
        }

        private void OnVPCPropertiesAvailable(QueryVPCPropertiesWorker.VPCPropertyData data)
        {
            var subnets = new List<Amazon.EC2.Model.Subnet>();
            foreach(var kvp in data.Subnets)
            {
                subnets.Add(kvp.Value);
            }

            _pageUI.SetAvailableSubnets(subnets);

            _pageUI.VPCSecurityGroups = data.SecurityGroups;
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

    }
}
