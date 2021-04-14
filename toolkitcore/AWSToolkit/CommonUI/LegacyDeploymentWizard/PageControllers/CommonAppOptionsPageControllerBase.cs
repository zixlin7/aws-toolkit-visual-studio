using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageWorkers;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI;
using log4net;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers
{
    public abstract class CommonAppOptionsPageControllerBase : IAWSWizardPageController
    {
        protected static readonly ILog LOGGER = LogManager.GetLogger(typeof(CommonAppOptionsPageControllerBase));

        protected CommonAppOptionsPage _pageUI;
        public static string[] AppParamKeys = new string[] 
        {
            CommonAppOptionsPage.APPPARAM1_KEY,
            CommonAppOptionsPage.APPPARAM2_KEY,
            CommonAppOptionsPage.APPPARAM3_KEY,
            CommonAppOptionsPage.APPPARAM4_KEY,
            CommonAppOptionsPage.APPPARAM5_KEY,
        };

        protected string _lastDeployedAccessKey;
        protected string _lastDeployedSecretKey;

        public static string DefaultHealthCheckUrl = "/";

        protected SettingsCollection.ObjectSettings _secretKeyCollection;

        object _syncLock = new object();
        bool _workersActive = false;
        protected bool WorkersActive
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

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Application Options";

        public string ShortPageTitle => null;

        public string PageDescription => "Set additional options and credentials for the deployed application.";

        public void ResetPage()
        {

        }

        protected abstract bool OnQueryPageActivation(AWSWizardConstants.NavigationReason navigationReason);

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return OnQueryPageActivation(navigationReason);
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new CommonAppOptionsPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onPropertyChanged);

                // these settings are based off VS project settings, so we can set once only
                string targetRuntime = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string;
                bool enable32BitApps = false;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications))
                    enable32BitApps = (bool)HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications];

                _pageUI.SetAppPoolSettings(targetRuntime, enable32BitApps);

                // the ability to use an iam user can be safely set here; only IAM user accounts whose keys are
                // held in the toolkit are valid and that won't change during the wizard's runtime
                var settingsCollection = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.SecretKeyRepository);
                if (settingsCollection != null && settingsCollection.Count > 0)
                    _secretKeyCollection = settingsCollection[ToolkitSettingsConstants.SecretKeyRepository];

                _pageUI.IAMSelectionAvailable = _secretKeyCollection != null && !_secretKeyCollection.IsEmpty;
            }

            return _pageUI;
        }

        protected abstract void OnPageActivated(AWSWizardConstants.NavigationReason navigationReason, bool isRedeploying);

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                bool isRedeploying = false;

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                    isRedeploying = (bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
                _pageUI.IsRedeploying = isRedeploying;

                _lastDeployedAccessKey = null;
                _lastDeployedSecretKey = null;
                PopulateAppParams(isRedeploying);

                // this customizes the 'use my account' button to show some indication of 'my'
                AccountViewModel account = HostingWizard.GetSelectedAccount();
                _pageUI.SelectedAccountName = account.AccountDisplayName;

                OnPageActivated(navigationReason, isRedeploying);
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
            bool pageComplete = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, pageComplete);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, pageComplete);
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        // only called if all tests on the common base have succeeded
        protected virtual bool OnQueryForwardsNavigationAllowed => true;

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return true;

                if (_pageUI.DataLoadPending || WorkersActive)
                    return false;

                bool ok = !string.IsNullOrEmpty(_pageUI.HealthCheckURL);
                if (ok)
                {
                    if (_pageUI.AppCredentialsMode == CommonAppOptionsPage.AppCredentials.UseSpecified)
                        ok = !string.IsNullOrEmpty(_pageUI.AccessKey) && !string.IsNullOrEmpty(_pageUI.SecretKey);
                    else
                        if (_pageUI.AppCredentialsMode == CommonAppOptionsPage.AppCredentials.UseIAM)
                            ok = !string.IsNullOrEmpty(_pageUI.SelectedIAMUserKey);

                    // if all good so far, allow derived controllers the final say
                    if (ok)
                        ok = OnQueryForwardsNavigationAllowed;
                }

                return ok;
            }
        }

        protected virtual void OnStorePageData() { }

        // stores common data; derived controllers should override OnStorePageData() to store controller-specific data
        // fields on display
        void StorePageData()
        {
            string targetRuntime = _pageUI.TargetRuntime;
            if (!string.IsNullOrEmpty(targetRuntime))
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = targetRuntime;
            else
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = null;

            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications] = _pageUI.Enable32BitApplications;

            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] = _pageUI.HealthCheckURL;

            IDictionary<string, string> appSettings;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings))
                appSettings = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] as IDictionary<string, string>;
            else
            {
                appSettings = new Dictionary<string, string>();
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] = appSettings;
            }

            foreach (var pk in AppParamKeys)
            {
                var setting = _pageUI.GetAppParam(pk);
                if (!string.IsNullOrEmpty(setting))
                    appSettings[pk] = _pageUI.GetAppParam(pk);
            }

            switch (_pageUI.AppCredentialsMode)
            {
                case CommonAppOptionsPage.AppCredentials.UseNone:
                    {
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = null;
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = null;
                    }
                    break;

                case CommonAppOptionsPage.AppCredentials.ReuseLast:
                    {
                        if (!string.IsNullOrEmpty(_lastDeployedAccessKey))
                        {
                            appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = _lastDeployedAccessKey;
                            appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = _lastDeployedSecretKey;
                        }
                        else
                        {
                            appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = null;
                            appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = null;
                        }
                    }
                    break;

                case CommonAppOptionsPage.AppCredentials.UseSpecified:
                    {
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = _pageUI.AccessKey;
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = _pageUI.SecretKey;
                    }
                    break;

                case CommonAppOptionsPage.AppCredentials.UseDeploymentAccount:
                    {
                        AccountViewModel selectedAccount = HostingWizard.GetSelectedAccount();
                        var profileProperties = selectedAccount.ProfileProperties;
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = profileProperties?.AccessKey;
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = profileProperties?.SecretKey;
                    }
                    break;

                case CommonAppOptionsPage.AppCredentials.UseIAM:
                    {
                        string accessKey = _pageUI.SelectedIAMUserKey;
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = accessKey;
                        appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = _secretKeyCollection[accessKey];
                    }
                    break;
            }

            OnStorePageData();
        }

        protected abstract void PopulateAppParams(bool isRedeploying);

        void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == CommonAppOptionsPage.uiProperty_CredentialsMode
                    && _pageUI.AppCredentialsMode == CommonAppOptionsPage.AppCredentials.UseIAM)
            {
                List<string> localKeys = new List<string>();
                foreach (string key in _secretKeyCollection.Keys)
                {
                    localKeys.Add(key);
                }

                WorkersActive = true;
                new FetchCompatibleIAMUsersWorker(HostingWizard.GetSelectedAccount(),
                                                  HostingWizard.GetSelectedRegion(), 
                                                  localKeys, 
                                                  LOGGER, 
                                                  new FetchCompatibleIAMUsersWorker.DataAvailableCallback(OnIAMAccountKeysAvailable));    
            }

            TestForwardTransitionEnablement();
        }

        void OnIAMAccountKeysAvailable(Dictionary<string, List<string>> iamAccounts)
        {
            _pageUI.IAMUserAccounts = iamAccounts;
            WorkersActive = false;
        }
    }
}
