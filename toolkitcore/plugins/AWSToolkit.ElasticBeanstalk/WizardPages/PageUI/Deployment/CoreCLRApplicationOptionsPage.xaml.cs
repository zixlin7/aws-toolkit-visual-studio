using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.ElasticBeanstalk.Model;
using System.Diagnostics;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for CoreCLRApplicationOptionsPage.xaml
    /// </summary>
    public partial class CoreCLRApplicationOptionsPage : INotifyPropertyChanged
    {
        readonly object _syncObj = new object();

        public static readonly string uiProperty_HealthCheckUrl = "HealthCheckUri";
        public static readonly string uiProperty_DeploymentVersionLabel = "DeploymentVersionLabel";
        public static readonly string uiProperty_EnableXRayDaemon = "EnableXRayDaemon";
        public static readonly string uiProperty_EnableEnhancedHealth = "EnableEnhancedHealth";

        public CoreCLRApplicationOptionsPage()
        {
            HealthCheckUri = "/";

            InitializeComponent();
            DataContext = this;
        }

        public CoreCLRApplicationOptionsPage(IAWSWizardPageController pageController)
            : this()
        {
            PageController = pageController;
        }

        public IAWSWizardPageController PageController { get; set; }

        public ObservableCollection<string> BuildConfigurations { get; set; }

        public string SelectedBuildConfiguration { get; set; }

        public string IISAppPath { get; set; }

        public string TargetFramework
        {
            get
            {
                if (_targetFramework.SelectedItem != null)
                    return (_targetFramework.SelectedItem as ComboBoxItem).Tag as string;

                return string.Empty;
            }
        }

        // use if redeploying an existing archive, we won't be doing a client side build
        public void ConfigureForAppVersionRedeployment()
        {
            _buildConfigurationsLabel.Visibility = Visibility.Collapsed;
            _buildConfigurations.Visibility = Visibility.Collapsed;

            _iisAppPathLabel.Visibility = Visibility.Collapsed;
            _iisAppPath.Visibility = Visibility.Collapsed;
        }

        // cannot rely on control visibility to determine single-vs-lb environment types
        // when we come to validation
        private bool IsSingleInstanceEnvironment { get; set; }

        public void ConfigureForEnvironmentType(bool isSingleInstanceEnvironment)
        {
            IsSingleInstanceEnvironment = isSingleInstanceEnvironment;

            _healthCheckFromInstanceMsg.Visibility = isSingleInstanceEnvironment
                ? Visibility.Visible
                : Visibility.Hidden;
            _healthCheckURL.Visibility = isSingleInstanceEnvironment 
                ? Visibility.Hidden 
                : Visibility.Visible;
        }

        public IDictionary<string, string> AppSettings
        {
            get => new Dictionary<string, string>();
            set
            {
            }
        }

        private string _healthCheckUri;
        public string HealthCheckUri 
        {
            get => _healthCheckUri;
            set
            {
                this._healthCheckUri = value;
                NotifyPropertyChanged(uiProperty_HealthCheckUrl);
            }
        }

        public bool HealthCheckUrlIsValid
        {
            get
            {
                // wpf style validation is not working and too much time spent trying to
                // figure out why - this is a hack to at least let us release the feature
                //return HasValidContent(_healthCheckURL);

                // page startup code can init uri to null (as opposed to empty string)
                // when it detects single instance env, so handle that as valid
                if (IsSingleInstanceEnvironment || HealthCheckUri == null)
                    return true;

                if (!string.IsNullOrEmpty(HealthCheckUri))
                {
                    if (Uri.IsWellFormedUriString(string.Format("http://test.elasticbeanstalk.com{0}", HealthCheckUri),
                                                  UriKind.Absolute))
                        return true;
                }

                return false;
            }
        }

        private string _deploymentVersionLabel;
        public string DeploymentVersionLabel
        {
            get => _deploymentVersionLabel;
            set
            {
                _deploymentVersionLabel = value;
                NotifyPropertyChanged(uiProperty_DeploymentVersionLabel);
            }
        }


        public void SetDefaultRuntimesOrFrameworks(string targetFramework, IDictionary<string, string> availableFrameworks)
        {
            ComboBoxItem frameworkToSelect = null;

            foreach (var key in availableFrameworks.Keys)
            {
                var item = new ComboBoxItem();
                item.Content = key;
                item.Tag = availableFrameworks[key];

                if (frameworkToSelect == null && key.Equals(targetFramework, StringComparison.Ordinal))
                    frameworkToSelect = item;

                _targetFramework.Items.Add(item);
            }

            if (frameworkToSelect != null)
                _targetFramework.SelectedItem = frameworkToSelect;
            else
                _targetFramework.SelectedIndex = _targetFramework.Items.Count - 1;
        }

        bool _versionFetchPending = false;
        public bool VersionFetchPending
        {
            get
            {
                lock (_syncObj)
                    return _versionFetchPending;
            }

            set
            {
                lock (_syncObj)
                    _versionFetchPending = value;
            }
        }

        public Visibility EnvironmentOptionsPanelVisibility
        {
            get
            {
                if (this.XRayPanelVisibility == Visibility.Visible || this.EnhancedHealthPanelVisibility == Visibility.Visible)
                    return Visibility.Visible;

                return Visibility.Hidden;
            }
        }

        Visibility _xrayPanelVisibility = Visibility.Visible;
        public Visibility XRayPanelVisibility
        {
            get => this._xrayPanelVisibility;
            set
            {
                this._xrayPanelVisibility = value;
                NotifyPropertyChanged("XRayPanelVisibility");
                NotifyPropertyChanged("EnvironmentOptionsPanelVisibility");
            }
        }

        Visibility _enhancedHealthPanelVisibility = Visibility.Visible;
        public Visibility EnhancedHealthPanelVisibility
        {
            get => this._enhancedHealthPanelVisibility;
            set
            {
                this._enhancedHealthPanelVisibility = value;
                NotifyPropertyChanged("EnhancedHealthPanelVisibility");
                NotifyPropertyChanged("EnvironmentOptionsPanelVisibility");
            }
        }


        private bool _enableXRayDaemon;
        public bool EnableXRayDaemon
        {
            get => this._enableXRayDaemon;
            set
            {
                this._enableXRayDaemon = value;
                NotifyPropertyChanged(uiProperty_EnableXRayDaemon);
            }
        }

        public void SetEnvironmentOptionsAvailability(bool xrayIsAvailable, bool enhancedHealthAvailable)
        {
            XRayPanelVisibility = xrayIsAvailable ? Visibility.Visible : Visibility.Collapsed;
            EnhancedHealthPanelVisibility = enhancedHealthAvailable ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool _enableEnhancedHealth;
        public bool EnableEnhancedHealth
        {
            get => this._enableEnhancedHealth;
            set
            {
                this._enableEnhancedHealth = value;
                NotifyPropertyChanged(uiProperty_EnableEnhancedHealth);
            }
        }

        public bool VersionLabelIsValid
        {
            get
            {
                // wpf style validation is not working and too much time spent trying to
                // figure out why - this is a hack to at least let us release the feature
                //return HasValidContent(_versionLabel);

                if (VersionFetchPending)
                    return true; // don't want ugly validation icon while we're loading

                var isValid = false;
                var version = DeploymentVersionLabel;
                if (!string.IsNullOrEmpty(version))
                {
                    if (ExistingVersionLabels != null && ExistingVersionLabels.Any())
                        isValid = ExistingVersionLabels.All(v => string.Compare(v.VersionLabel, version, StringComparison.OrdinalIgnoreCase) != 0);
                    else
                        isValid = true;
                }

                return isValid;
            }
        }

        public void LoadExistingVersions()
        {
            this.VersionFetchPending = true;

            var selectedAccount = PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            var selectedRegion = PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
            var selectedApplication = PageController.HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
            new QueryExistingApplicationVersionsWorker(selectedAccount,
                                                       selectedRegion,
                                                       selectedApplication,
                                                       PageController.HostingWizard.Logger,
                                                       OnVersionsAvailable);
        }

        internal IEnumerable<ApplicationVersionDescription> ExistingVersionLabels { get; set; }

        void OnVersionsAvailable(IEnumerable<ApplicationVersionDescription> existingVersions)
        {
            ExistingVersionLabels = existingVersions;
            VersionFetchPending = false;
            NotifyPropertyChanged(uiProperty_DeploymentVersionLabel);
        }

        private void _healthCheckURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized)
            {
                NotifyPropertyChanged(uiProperty_HealthCheckUrl);
                // this is a hack to get around wpf validators just not working for no obvious reason
                _healthCheckURLInvalid.Visibility = HealthCheckUrlIsValid ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void _versionLabel_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized)
            {
                NotifyPropertyChanged(uiProperty_DeploymentVersionLabel);
                // this is a hack to get around wpf validators just not working for no obvious reason
                _versionLabelInvalid.Visibility = VersionLabelIsValid ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Utility.LaunchXRayHelp(true);
        }

        private void Hyperlink_RequestNavigateEnhancedHealth(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString()));
        }
    }
}
