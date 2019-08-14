using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.IdentityManagement.Model;
using AWSDeployment;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    internal class PermissionsPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PermissionsPageController));

        private PermissionsPage _pageUI;
        private string _lastSeenAccount = string.Empty;
        private string _lastSeenRegion = string.Empty;

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

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription => "Select roles granting permissions to your deployed application and for the service to monitor resources.";

        public string PageGroup => DeploymentWizardPageGroups.PermissionsGroup;

        public string PageID => GetType().FullName;

        public string PageTitle => "Permissions";

        public string ShortPageTitle => null;

        public bool AllowShortCircuit()
        {
            return true;
        }

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
                _pageUI = new PermissionsPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                var selectedRegion = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;

                if (!string.Equals(selectedAccount.AccountDisplayName, _lastSeenAccount, StringComparison.CurrentCulture)
                        || !string.Equals(selectedRegion.SystemName, _lastSeenRegion, StringComparison.CurrentCulture))
                {
                    _lastSeenAccount = selectedAccount.AccountDisplayName;

                    LoadExistingRoles(selectedAccount, selectedRegion);
                    this._pageUI.InitializeIAM(selectedAccount, selectedRegion);
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
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            // all of this page's data is optional provided no workers are running
            var enableNext = !WorkersActive;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enableNext);
        }

        private void StorePageData()
        {
            var selectedInstanceProfile = _pageUI == null ? BeanstalkParameters.DefaultRoleName : _pageUI.SelectedInstanceProfile;
            var selectedServiceRole = _pageUI == null ? BeanstalkParameters.DefaultServiceRoleName : _pageUI.SelectedServicePermissionRole;
            var selectedPolicyTemplates = _pageUI == null ? null : _pageUI.SelectedPolicyTemplates;

            HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName] = selectedInstanceProfile;
            HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ServiceRoleName] = selectedServiceRole;
            HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_PolicyTemplates] = selectedPolicyTemplates;
        }

        private void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        void LoadExistingRoles(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region)
        {
            Interlocked.Increment(ref _workersActive);
            new QueryServiceRolesWorker(selectedAccount,
                                        region,
                                        HostingWizard.Logger,
                                        OnRolesAvailable);
            TestForwardTransitionEnablement();
        }

        void OnRolesAvailable(IEnumerable<Role> roles)
        {
            _pageUI.SetServicePermissionRoles(roles);
            Interlocked.Decrement(ref _workersActive);
            TestForwardTransitionEnablement();
        }

    }
}
