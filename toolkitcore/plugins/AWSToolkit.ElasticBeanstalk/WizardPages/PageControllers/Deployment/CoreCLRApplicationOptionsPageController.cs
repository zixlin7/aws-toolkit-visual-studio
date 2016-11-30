using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return DeploymentWizardPageGroups.AppOptionsGroup; }
        }

        public string PageTitle
        {
            get { return "Application Options"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Set additional build and deployment options for your application."; }
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
                //var enable32BitApps = false;
                //if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications))
                //    enable32BitApps = (bool)HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications];
                //_pageUI.Enable32BitAppPool = enable32BitApps;

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
        bool _originalEnable32bitAppPool = false;

        bool _needToFetchData;

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            _needToFetchData = false;

            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
                    _pageUI.IISAppPath = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;

                if (HostingWizard.GetProperty<bool>(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                {
                    var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                    var selectedRegion = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
                    var selectedEnvironment = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string;

                    if (!string.Equals(selectedAccount.AccountDisplayName, _lastSeenAccount, StringComparison.CurrentCulture)
                        || !string.Equals(selectedRegion.SystemName, _lastSeenRegion, StringComparison.CurrentCulture)
                        || !string.Equals(selectedEnvironment, _lastSeenEnvironment, StringComparison.CurrentCulture))
                    {
                        _lastSeenAccount = selectedAccount.AccountDisplayName;
                        _lastSeenRegion = selectedRegion.SystemName;
                        _lastSeenEnvironment = selectedEnvironment;

                        var applicationName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
                        LoadEnvironmentSettings(selectedAccount, selectedRegion, applicationName, selectedEnvironment);
                    }
                }
                else
                {
                    _pageUI.ConfigureForEnvironmentType(DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard));
                    // always load versions as we could be deploying a new environment for an existing app; the load will
                    // yield an empty version collection for as-yet-unknown apps
                    _pageUI.LoadExistingVersions();                    
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
                                }
                            }
                        }
                    }
                    finally
                    {
                        this._needToFetchData = false;
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        this._originalHealthCheckUri = healthCheckUri;
                        this._pageUI.HealthCheckUri = healthCheckUri;

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

                return false;
            }
        }

        void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
