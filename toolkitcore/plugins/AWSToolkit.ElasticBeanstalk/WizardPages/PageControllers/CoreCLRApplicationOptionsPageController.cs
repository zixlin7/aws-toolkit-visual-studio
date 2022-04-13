using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.Regions;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    public class CoreCLRApplicationOptionsPageController : IAWSWizardPageController
    {
        protected CoreCLRApplicationOptionsPage _pageUI;
        private bool RedeployingAppVersion = false;

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => DeploymentWizardPageGroups.AppOptionsGroup;

        public string PageTitle => "Application Options";

        public string ShortPageTitle => null;

        public string PageDescription => "Set additional build and deployment options for your application.";

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_ProjectType))
            {
                var projectType = HostingWizard.GetProperty(DeploymentWizardProperties.SeedData.propkey_ProjectType) as string;
                return projectType.Equals(DeploymentWizardProperties.NetCoreWebProject, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new CoreCLRApplicationOptionsPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;

                var targetRuntime = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string;

                var availableFrameworks = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ProjectFrameworks] as Dictionary<string, string>;
                _pageUI.SetDefaultRuntimesOrFrameworks(targetRuntime, availableFrameworks);

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion))
                    RedeployingAppVersion = (bool)HostingWizard[DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion];

                if (HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack] is bool isLinuxDeployment)
                {
                    _pageUI.IsLinuxDeployment = isLinuxDeployment;
                    _pageUI.BuildSelfContainedBundle = SelfContainedDefaultFor(_pageUI.TargetFramework, _pageUI.IsLinuxDeployment);
                }

                if (!RedeployingAppVersion)
                {
                    var buildConfigurations = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ProjectBuildConfigurations] as IDictionary<string, string>;
                    var activeBuildConfiguration = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ActiveBuildConfiguration] as string;
                    string lastDeployedBuildConfiguration = null;
                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration))
                        lastDeployedBuildConfiguration = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] as string;

                    _pageUI.BuildConfigurations = new ObservableCollection<string>(buildConfigurations.Keys);
                    _pageUI.SelectedBuildConfiguration = DeploymentWizardHelper.SelectDeploymentBuildConfiguration(buildConfigurations.Keys, lastDeployedBuildConfiguration, activeBuildConfiguration);
                }
                else
                    _pageUI.ConfigureForAppVersionRedeployment();

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel))
                    _pageUI.DeploymentVersionLabel = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;
            }

            return _pageUI;
        }

        private bool SelfContainedDefaultFor(string targetFramework, bool isLinuxDeployment) => targetFramework.MatchesFramework(Frameworks.Net60) && !isLinuxDeployment;

        string _lastSeenAccount;
        string _lastSeenRegion;
        string _lastSeenEnvironment;

        readonly Dictionary<string, string> _originalAppSettings = new Dictionary<string, string>();
        string _originalHealthCheckUri = "/";
        bool _originalEnableXRayDaemon = false;
        bool _originalEnableEnhancedHealth = false;
        string _originalProxyServer = Amazon.ElasticBeanstalk.Tools.EBConstants.PROXY_SERVER_NGINX;

        bool _needToFetchData;

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            _needToFetchData = false;

            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
                    _pageUI.IISAppPath = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;

                if (HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack] is bool isLinuxDeployment
                    && isLinuxDeployment != _pageUI.IsLinuxDeployment) {
                    _pageUI.IsLinuxDeployment = isLinuxDeployment;
                    _pageUI.BuildSelfContainedBundle = SelfContainedDefaultFor(_pageUI.TargetFramework, _pageUI.IsLinuxDeployment);
                }


                DeploymentWizardHelper.ValidBeanstalkOptions validOptions = null;
                var selectedAccount = HostingWizard.GetSelectedAccount();
                var selectedRegion = HostingWizard.GetSelectedRegion();

                var beanstalkClient = selectedAccount.CreateServiceClient<AmazonElasticBeanstalkClient>(selectedRegion);

                if (HostingWizard.GetProperty<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                {
                    var selectedEnvironment = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string;
                    var applicationName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;

                    if (!string.Equals(selectedAccount.DisplayName, _lastSeenAccount, StringComparison.CurrentCulture)
                        || !string.Equals(selectedRegion.Id, _lastSeenRegion, StringComparison.CurrentCulture)
                        || !string.Equals(selectedEnvironment, _lastSeenEnvironment, StringComparison.CurrentCulture))
                    {
                        _lastSeenAccount = selectedAccount.DisplayName;
                        _lastSeenRegion = selectedRegion.Id;
                        _lastSeenEnvironment = selectedEnvironment;

                        LoadEnvironmentSettings(selectedAccount, selectedRegion, applicationName, selectedEnvironment);
                    }

                    validOptions = DeploymentWizardHelper.TestForValidOptionsForEnvironnment(beanstalkClient, applicationName, selectedEnvironment);
                }
                else
                {
                    _pageUI.ConfigureForEnvironmentType(DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard));
                    // always load versions as we could be deploying a new environment for an existing app; the load will
                    // yield an empty version collection for as-yet-unknown apps
                    _pageUI.LoadExistingVersions();

                    var selectedSolutionStack = HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] as string;
                    validOptions = DeploymentWizardHelper.TestForValidOptionsForEnvironnment(beanstalkClient, selectedSolutionStack);
                }

                var xrayAvailableInRegion = (bool)HostingWizard.GetProperty(BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_XRayAvailable);
                _pageUI.SetEnvironmentOptionsAvailability(xrayAvailableInRegion && validOptions.XRay, validOptions.EnhancedHealth);
                if (xrayAvailableInRegion && validOptions.XRay)
                {
                    var enableXRayDaemon = false;
                    if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon))
                        enableXRayDaemon = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon];

                    _pageUI.EnableXRayDaemon = enableXRayDaemon;
                }

                if (validOptions.EnhancedHealth)
                {
                    var enableEnhancedHealth = false;
                    if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableEnhancedHealth))
                        enableEnhancedHealth = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableEnhancedHealth];

                    _pageUI.EnableEnhancedHealth = enableEnhancedHealth;
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
            return IsForwardsNavigationAllowed;
        }

        void LoadEnvironmentSettings(AccountViewModel selectedAccount, ToolkitRegion region, 
            string applicationName, string environmentName)
        {
            try
            {
                this._needToFetchData = true;
                TestForwardTransitionEnablement();

                var beanstalkClient = selectedAccount.CreateServiceClient<AmazonElasticBeanstalkClient>(region);

                var request = new DescribeConfigurationSettingsRequest
                {
                    ApplicationName = applicationName,
                    EnvironmentName = environmentName
                };

                beanstalkClient.DescribeConfigurationSettingsAsync(request).ContinueWith(task =>
                {
                    var healthCheckUri = "/";
                    var isSingleInstanceEnvironment = false;
                    var enableXRayDaemon = false;
                    var enableEnhancedHealth = false;
                    var proxyServer = Amazon.ElasticBeanstalk.Tools.EBConstants.PROXY_SERVER_NGINX;
                    var appSettings = new Dictionary<string, string>();
                    this._originalAppSettings.Clear();

                    try
                    {
                        var response = task.Result;
                        foreach (var setting in response.ConfigurationSettings)
                        {
                            if (string.Equals(setting.EnvironmentName, environmentName))
                            {
                                foreach (var optionSetting in setting.OptionSettings)
                                {
                                    if (string.Equals(optionSetting.Namespace, "aws:elasticbeanstalk:application:environment", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (!string.IsNullOrEmpty(optionSetting.Value))
                                            appSettings[optionSetting.OptionName] = optionSetting.Value;

                                        continue;
                                    }

                                    if (optionSetting.Namespace == "aws:elasticbeanstalk:environment")
                                    {
                                        if (optionSetting.OptionName == "EnvironmentType")
                                            isSingleInstanceEnvironment = string.Equals("SingleInstance", optionSetting.Value, StringComparison.OrdinalIgnoreCase);

                                        continue;
                                    }

                                    if (optionSetting.Namespace == "aws:elasticbeanstalk:application")
                                    {
                                        if (optionSetting.OptionName == "Application Healthcheck URL")
                                            healthCheckUri = optionSetting.Value;

                                        continue;
                                    }

                                    if (optionSetting.Namespace == "aws:elasticbeanstalk:xray")
                                    {
                                        if (optionSetting.OptionName == "XRayEnabled")
                                            bool.TryParse(optionSetting.Value, out enableXRayDaemon);

                                        continue;
                                    }

                                    if (optionSetting.Namespace == "aws:elasticbeanstalk:healthreporting:system")
                                    {
                                        if (optionSetting.OptionName == "SystemType" && string.Equals(optionSetting.Value, "enhanced", StringComparison.OrdinalIgnoreCase))
                                            enableEnhancedHealth = true;

                                        continue;
                                    }

                                    if (optionSetting.Namespace == "aws:elasticbeanstalk:environment:proxy")
                                    {
                                        if (optionSetting.OptionName == "ProxyServer")
                                            proxyServer = optionSetting.Value;

                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        this._needToFetchData = false;
                    }

                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                    {
                        this._originalHealthCheckUri = healthCheckUri;
                        this._pageUI.HealthCheckUri = healthCheckUri;

                        this._originalEnableXRayDaemon = enableXRayDaemon;
                        this._pageUI.EnableXRayDaemon = enableXRayDaemon;

                        this._originalEnableEnhancedHealth = enableEnhancedHealth;
                        this._pageUI.EnableEnhancedHealth = enableEnhancedHealth;

                        this._originalProxyServer = proxyServer;
                        this._pageUI.SelectedReverseProxyOption = proxyServer;

                        this._pageUI.ConfigureForEnvironmentType(isSingleInstanceEnvironment);

                        this._pageUI.AppSettings = appSettings;
                        foreach (var kvp in appSettings)
                            this._originalAppSettings[kvp.Key] = kvp.Value;

                        _pageUI.LoadExistingVersions();

                        TestForwardTransitionEnablement();
                    }));
                });
            }
            catch (Exception e)
            {
                HostingWizard.Logger.Error(GetType().FullName + ", exception in LoadCurrentAppSettings", e);
            }
        }


        public void TestForwardTransitionEnablement()
        {
            bool pageComplete = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, pageComplete);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, pageComplete);
        }

        public bool AllowShortCircuit()
        {
            return false;
        }

        private bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false; // this is not an optional page

                if (_needToFetchData || _pageUI.VersionFetchPending)
                    return false;

                return _pageUI.HealthCheckUrlIsValid && _pageUI.VersionLabelIsValid;
            }
        }

        private void StorePageData()
        {
            string targetFramework = _pageUI.TargetFramework;
            if (!string.IsNullOrEmpty(targetFramework))
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = targetFramework;
            else
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = null;

            // Do not emit 32 bit applications related config -- Linux containers reject this, and IIS for .NET Core doesn't use it
            HostingWizard.SetProperty(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications, null);

            if (!this._pageUI.IsLinuxDeployment)
            {
                if (string.IsNullOrEmpty(_pageUI.IISAppPath) || string.Equals(_pageUI.IISAppPath, "/"))
                    HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] = AWSDeployment.CommonParameters.DefaultIisAppPathFormat;
                else
                    HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] = _pageUI.IISAppPath;
            }

            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] = _pageUI.HealthCheckUri;
            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] = _pageUI.SelectedBuildConfiguration;

            // currently always an empty dictionary for coreclr projects
            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] = this._pageUI.AppSettings;

            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] = _pageUI.DeploymentVersionLabel;
            HostingWizard[BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_AppOptionsUpdated] = this.HasEnvironmentSettingsChanged;

            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon] = this._pageUI.EnableXRayDaemon;
            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableEnhancedHealth] = this._pageUI.EnableEnhancedHealth;

            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_BuildSelfContainedBundle] = this._pageUI.BuildSelfContainedBundle;

            if (this._pageUI.IsLinuxDeployment)
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_ReverseProxyMode] = this._pageUI.SelectedReverseProxyOption;
            }
        }

        private bool HasEnvironmentSettingsChanged
        {
            get
            {
                var newSettings = this._pageUI.AppSettings;
                if (newSettings.Count != this._originalAppSettings.Count)
                    return true;

                foreach(var kvp in newSettings)
                {
                    if (!this._originalAppSettings.ContainsKey(kvp.Key))
                        return true;

                    if(!string.Equals(kvp.Value, this._originalAppSettings[kvp.Key]))
                        return true;
                }

                if (this._pageUI.HealthCheckUri != this._originalHealthCheckUri)
                    return true;

                if (this._pageUI.EnableXRayDaemon != this._originalEnableXRayDaemon)
                    return true;

                if (this._pageUI.EnableEnhancedHealth != this._originalEnableEnhancedHealth)
                    return true;

                return false;
            }
        }

        void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
