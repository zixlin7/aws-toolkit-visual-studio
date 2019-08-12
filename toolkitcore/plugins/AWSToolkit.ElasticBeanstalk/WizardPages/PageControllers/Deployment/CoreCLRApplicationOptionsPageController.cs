﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.Deployment
{
    internal class CoreCLRApplicationOptionsPageController : IAWSWizardPageController
    {
        private CoreCLRApplicationOptionsPage _pageUI;
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


        string _lastSeenAccount;
        string _lastSeenRegion;
        string _lastSeenEnvironment;

        readonly Dictionary<string, string> _originalAppSettings = new Dictionary<string, string>();
        string _originalHealthCheckUri = "/";
        bool _originalEnableXRayDaemon = false;
        bool _originalEnableEnhancedHealth = false;

        bool _needToFetchData;

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            _needToFetchData = false;

            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
                    _pageUI.IISAppPath = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;

                DeploymentWizardHelper.ValidBeanstalkOptions validOptions = null;
                var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                var selectedRegion = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;

                if (HostingWizard.GetProperty<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                {
                    var selectedEnvironment = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string;
                    var applicationName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;

                    if (!string.Equals(selectedAccount.AccountDisplayName, _lastSeenAccount, StringComparison.CurrentCulture)
                        || !string.Equals(selectedRegion.SystemName, _lastSeenRegion, StringComparison.CurrentCulture)
                        || !string.Equals(selectedEnvironment, _lastSeenEnvironment, StringComparison.CurrentCulture))
                    {
                        _lastSeenAccount = selectedAccount.AccountDisplayName;
                        _lastSeenRegion = selectedRegion.SystemName;
                        _lastSeenEnvironment = selectedEnvironment;

                        LoadEnvironmentSettings(selectedAccount, selectedRegion, applicationName, selectedEnvironment);
                    }

                    validOptions = DeploymentWizardHelper.TestForValidOptionsForEnvironnment(DeploymentWizardHelper.GetBeanstalkClient(selectedAccount, selectedRegion), applicationName, selectedEnvironment);
                }
                else
                {
                    _pageUI.ConfigureForEnvironmentType(DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard));
                    // always load versions as we could be deploying a new environment for an existing app; the load will
                    // yield an empty version collection for as-yet-unknown apps
                    _pageUI.LoadExistingVersions();

                    var selectedSolutionStack = HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] as string;
                    validOptions = DeploymentWizardHelper.TestForValidOptionsForEnvironnment(DeploymentWizardHelper.GetBeanstalkClient(selectedAccount, selectedRegion), selectedSolutionStack);
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

        void LoadEnvironmentSettings(AccountViewModel selectedAccount, RegionEndPointsManager.RegionEndPoints region, 
            string applicationName, string environmentName)
        {
            try
            {
                this._needToFetchData = true;
                TestForwardTransitionEnablement();

                var beanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(selectedAccount, region);

                var request = new DescribeConfigurationSettingsRequest
                {
                    ApplicationName = applicationName,
                    EnvironmentName = environmentName
                };

                beanstalkClient.DescribeConfigurationSettingsAsync(request).ContinueWith(task =>
                {
                    var healthCheckUri = "/";
                    var enable32Bit = false;
                    var isSingleInstanceEnvironment = false;
                    var enableXRayDaemon = false;
                    var enableEnhancedHealth = false;
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

                                    if (optionSetting.Namespace == "aws:elasticbeanstalk:container:dotnet:apppool")
                                    {
                                        if (optionSetting.OptionName == "Enable 32-bit Applications")
                                            bool.TryParse(optionSetting.Value, out enable32Bit);

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

            //HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications] = _pageUI.Enable32BitAppPool;
            if (string.IsNullOrEmpty(_pageUI.IISAppPath) || string.Equals(_pageUI.IISAppPath, "/"))
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] = AWSDeployment.CommonParameters.DefaultIisAppPathFormat;
            else
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] = _pageUI.IISAppPath;

            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] = _pageUI.HealthCheckUri;
            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] = _pageUI.SelectedBuildConfiguration;

            // currently always an empty dictionary for coreclr projects
            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] = this._pageUI.AppSettings;

            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] = _pageUI.DeploymentVersionLabel;
            HostingWizard[BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_AppOptionsUpdated] = this.HasEnvironmentSettingsChanged;

            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon] = this._pageUI.EnableXRayDaemon;
            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableEnhancedHealth] = this._pageUI.EnableEnhancedHealth;
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
