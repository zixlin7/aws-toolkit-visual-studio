using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    class SecurityOptionsPageController : IAWSWizardPageController
    {
        SecurityOptionsPage _pageUI;
        bool _pageDataInitialized = false;

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
            get { return "Security"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Define the key pair and security group (firewall) options for the instance(s)."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new SecurityOptionsPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }
            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            FeatureViewModel ec2fvm = HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel;
            if (!_pageDataInitialized)
            {
                PopulateExistingKeyPairs(ec2fvm.EC2Client);                
                _pageUI.AllowKeyPairCreation = !ec2fvm.AccountViewModel.Restrictions.Contains("IsGovCloudAccount");
                _pageDataInitialized = true;
            }

            PopulateExistingGroups(ec2fvm.EC2Client);
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            bool enableNext = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enableNext);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        void StorePageData()
        {
            if (_pageUI == null)
                return;

            SecurityOptionsPage.KeyPairSelectionMode keyPairMode;
            string pairName;
            _pageUI.GetSelectedKeyPairOptions(out keyPairMode, out pairName);
            if (keyPairMode != SecurityOptionsPage.KeyPairSelectionMode.noPair)
            {
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_CreatePair] 
                    = keyPairMode == SecurityOptionsPage.KeyPairSelectionMode.createPair;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_KeyPair] = pairName;
            }
            else
            {
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_CreatePair] = null;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_KeyPair] = null;
            }

            if (!_pageUI.CreateSecurityGroup)
            {
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupName] = null;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupPermissions] = null;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_Groups] = _pageUI.SelectedGroups;
            }
            else
            {
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_Groups] = null;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupName] = _pageUI.GroupEditor.GroupName;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupDescription] = _pageUI.GroupEditor.GroupDescription;
                HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupPermissions] = _pageUI.GroupEditor.GroupPermissions;
            }
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI != null)
                    return _pageUI.IsValidToMoveOffPage;
                else
                {
                    // tweak to allow quick-launch page to enable Launch on its own authority; if the user has
                    // gone beyond quick-launch, we have mandatory fields
                    if (HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch))
                        return (bool)HostingWizard[LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch];
                    else
                        return false;
                }
            }
        }

        void PopulateExistingKeyPairs(IAmazonEC2 ec2Client)
        {
            var account = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as Account.AccountViewModel;
            if (account == null)
                return;

            var region = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
            if (region == null)
                return;

            QueryKeyPairNamesWorker worker
                = new QueryKeyPairNamesWorker(account,
                                              region.SystemName,
                                              ec2Client,
                                              HostingWizard.Logger,
                                              new QueryKeyPairNamesWorker.DataAvailableCallback(OnKeyPairNamesAvailable));
        }

        void OnKeyPairNamesAvailable(ICollection<string> keypairNames, ICollection<string> keyPairsStoredInToolkit)
        {
            _pageUI.SetExistingKeyPairs(keypairNames, keyPairsStoredInToolkit);
        }

        void PopulateExistingGroups(IAmazonEC2 ec2Client)
        {
            var subnet = HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet] as SubnetWrapper;
            string vpcId = null;
            if (subnet != null)
                vpcId = subnet.VpcId;
            QuerySecurityGroupsWorker worker
                = new QuerySecurityGroupsWorker(ec2Client,
                                                vpcId,
                                                HostingWizard.Logger,
                                                new QuerySecurityGroupsWorker.DataAvailableCallback(OnSecurityGroupsAvailable));
        }

        void OnSecurityGroupsAvailable(ICollection<SecurityGroup> securityGroups)
        {
            this._pageUI.ExistingGroups = securityGroups;
        }

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // don't care what changed
            TestForwardTransitionEnablement();
        }
    }
}
