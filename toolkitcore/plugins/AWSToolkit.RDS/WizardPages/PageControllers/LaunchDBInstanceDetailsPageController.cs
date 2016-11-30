using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.WizardPages;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class LaunchDBInstanceDetailsPageController : IAWSWizardPageController
    {
        LaunchDBInstanceDetailsPage _pageUI;
        string _lastSeenEngineType = string.Empty;

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
            get { return "DB Engine Instance Options"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Configure your DB engine instance."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new LaunchDBInstanceDetailsPage();
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            var engineVersions = HostingWizard[RDSWizardProperties.SeedData.propkey_DBEngineVersions] as List<DBEngineVersionWrapper>;
            if (string.Compare(engineVersions[0].Title, _lastSeenEngineType, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                // first time in or user has gone back to preceding page and changed engine class, need to re-initialize
                _pageUI.EngineVersions = engineVersions.OrderByDescending(x => x.EngineVersion.EngineVersion).ToList();
                _lastSeenEngineType = engineVersions[0].Title;
                _pageUI.RefreshContent();
            }

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                StorePageData();

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed();
        }

        public void TestForwardTransitionEnablement()
        {
            bool allow = IsForwardsNavigationAllowed();
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, allow);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, allow);
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        bool IsForwardsNavigationAllowed()
        {
            if (_pageUI == null)
                return false; 

            return !string.IsNullOrEmpty(_pageUI.LicenseModel)
                && _pageUI.SelectedVersion != null
                && _pageUI.InstanceClass != null
                && _pageUI.Storage != -1
                && !string.IsNullOrEmpty(_pageUI.DBInstanceIdentifier)
                && string.IsNullOrEmpty(_pageUI.ValidateDBInstanceIdentifier())
                && !string.IsNullOrEmpty(_pageUI.MasterUserName)
                && string.IsNullOrEmpty(_pageUI.ValidateUserName())
                && !string.IsNullOrEmpty(_pageUI.MasterUserPassword)
                && _pageUI.IsPasswordValid;
        }

        void _pageUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        void StorePageData()
        {
            HostingWizard[RDSWizardProperties.EngineProperties.propkey_DBEngine] = _lastSeenEngineType;
            HostingWizard[RDSWizardProperties.EngineProperties.propkey_EngineVersion] = _pageUI.SelectedVersion;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_LicenseModel] = _pageUI.LicenseModel;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_InstanceClass] = _pageUI.InstanceClass;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade] = _pageUI.AutoUpgradeMinorVersions;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBInstanceIdentifier] = _pageUI.DBInstanceIdentifier;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MasterUserName] = _pageUI.MasterUserName;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword] = _pageUI.MasterUserPassword;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MultiAZ] = _pageUI.IsMultiAZ;
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_Storage] = _pageUI.Storage;
        }
    }
}
