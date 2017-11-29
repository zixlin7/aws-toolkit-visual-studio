using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    internal class EnvironmentPageController : IAWSWizardPageController
    {
        EnvironmentPage _pageUI = null;
        object _syncLock = new object();

        bool _workersActive = false;
        public bool WorkersActive
        {
            get
            {
                bool ret;
                lock (_syncLock)
                    ret = _workersActive;
                return ret;
            }
            set
            {
                lock (_syncLock)
                    _workersActive = value;
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
            get { return "Environment"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Select or define an environment in which the application will run."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return IsWizardInBeanstalkMode;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new EnvironmentPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }
            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                _pageUI.UserCreatingNewApp = !((bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy]);
                PopulateEnvironments();
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
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            bool enableNext = IsForwardsNavigationAllowed;

            // downstream pages will want to inspect our properties when host wizard attempts
            // to enable Finish too
            if (enableNext)
                StorePageData();

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RefreshEnvironments")
                PopulateEnvironments();

            TestForwardTransitionEnablement();
        }

        bool IsWizardInBeanstalkMode
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.BeanstalkServiceName;
            }
        }

        void StorePageData()
        {
            if (!IsWizardInBeanstalkMode) 
                return;

            // Finish can't be pressed without our UI having been shown so we're safe to access it
            HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] = _pageUI.EnvironmentName;
            if (_pageUI.CreateNewEnvironment)
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv] = true;
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvDescription] = _pageUI.EnvironmentDescription;
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName] = _pageUI.CName;
                HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy] = false;
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType] = _pageUI.EnvironmentType;
            }
            else
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv] = false;
                HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy] = true;
            }
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (!IsWizardInBeanstalkMode)
                    return true; 

                if (_pageUI == null)
                    return false;

                if (WorkersActive)
                    return false;

                if (_pageUI.CreateNewEnvironment)
                    return _pageUI.NewEnvironmentNameIsValid && _pageUI.CNameIsValid;
                else
                    return _pageUI.SelectedExistingEnvironmentIsValid;
            }
        }

        void PopulateEnvironments()
        {
            // always populate so user can elect to wait and go back/fwd waiting on an environment
            AccountViewModel selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            string appName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;

            WorkersActive = true;
            new QueryEnvironmentsForAppWorker(selectedAccount,
                                              HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                                  as RegionEndPointsManager.RegionEndPoints,
                                              appName,
                                              HostingWizard.Logger,
                                              new QueryEnvironmentsForAppWorker.DataAvailableCallback(OnEnvironmentsAvailable));

            this._pageUI.ExistingEnvironments = null;
            TestForwardTransitionEnablement();
        }

        void OnEnvironmentsAvailable(IEnumerable<EnvironmentDescription> environments)
        {
            _pageUI.ExistingEnvironments = environments;
            WorkersActive = false;
            TestForwardTransitionEnablement();
        }
    }
}
