using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;

using Amazon.ElasticBeanstalk.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.EC2;
using System.Windows.Controls;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    internal class VpcOptionsPageController : IAWSWizardPageController
    {
        VpcOptionsPage _pageUI = null;
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

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "VPC Options"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Set Amazon VPC options for the deployed application."; }
        }

        public void ResetPage()
        {

        }


        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (IsWizardInBeanstalkMode && (bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv])
                return ((bool)HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC]);

            return false;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new VpcOptionsPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
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

            if (!(bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv])
                return true;

            bool vpcSelected = HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC)
                                && (bool)HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC];

            if (vpcSelected)
            {
                if (_pageUI == null)
                    return false;
                else
                {
                    if (!IsSingleInstanceEnvironmentType)
                        return this._pageUI.SelectedVPCId != null
                               && this._pageUI.SelectedVPCSecurityGroupId != null
                               && this._pageUI.SelectedInstanceSubnetId != null
                               && this._pageUI.SelectedELBSubnetId != null;
                    else
                        return this._pageUI.SelectedVPCId != null
                               && this._pageUI.SelectedVPCSecurityGroupId != null
                               && this._pageUI.SelectedInstanceSubnetId != null;
                }
            }
            else
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

        #endregion

        bool IsWizardInBeanstalkMode
        {
            get
            {
                var service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.BeanstalkServiceName;
            }
        }

        internal bool IsSingleInstanceEnvironmentType
        {
            get
            {
                if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType))
                    return ((string) HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType])
                            .Equals(BeanstalkConstants.EnvType_SingleInstance);

                return false;
            }
        }
        void StorePageData()
        {
            if (!IsWizardInBeanstalkMode || _pageUI == null)
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

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                                                 new QueryExistingVPCsWorker.DataAvailableCallback(OnVPCsAvailable));

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
                                                 new QueryVPCPropertiesWorker.DataAvailableCallback(OnVPCPropertiesAvailable));

            _pageUI.VPCSecurityGroups = null;
            _pageUI.InstanceSubnets = null;
            _pageUI.ELBSubnets = null;
        }

        private void OnVPCPropertiesAvailable(QueryVPCPropertiesWorker.VPCPropertyData data)
        {
            _pageUI.InstanceSubnets = data.Subnets;
            _pageUI.ELBSubnets = data.Subnets;
            _pageUI.VPCSecurityGroups = data.SecurityGroups;
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

    }
}
