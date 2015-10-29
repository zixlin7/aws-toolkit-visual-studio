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
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for ApplicationOptionsPage.xaml
    /// </summary>
    public partial class ApplicationOptionsPage : INotifyPropertyChanged
    {
        readonly object _syncObj = new object();

        public static readonly string uiProperty_HealthCheckUrl = "HealthCheckUri";
        public static readonly string uiProperty_Enable32BitAppPool = "Enable32BitAppPool";
        public static readonly string uiProperty_DeploymentVersionLabel = "DeploymentVersionLabel";

        public ApplicationOptionsPage()
        {
            HealthCheckUri = "/";

            InitializeComponent();
            DataContext = this;
        }

        public ApplicationOptionsPage(IAWSWizardPageController pageController)
            : this()
        {
            PageController = pageController;

            if (pageController.HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_ShowV2RuntimeOnly)
                    && (bool)pageController.HostingWizard[DeploymentWizardProperties.AppOptions.propkey_ShowV2RuntimeOnly])
            {
                // can't modify collection whilst enumerating
                var toRemove = _targetRuntime.Items.Cast<ComboBoxItem>().Where(cbi => (cbi.Tag as string).StartsWith("4")).ToList();

                foreach (var cbi in toRemove)
                {
                    _targetRuntime.Items.Remove(cbi);
                }
            }
        }

        public IAWSWizardPageController PageController { get; set; }

        public ObservableCollection<string> BuildConfigurations { get; set; }

        public string SelectedBuildConfiguration { get; set; }

        public string IISAppPath { get; set; }

        private bool _enable32BitAppPool;
        public bool Enable32BitAppPool
        {
            get { return _enable32BitAppPool; }
            set
            {
                this._enable32BitAppPool = value;
                NotifyPropertyChanged(uiProperty_Enable32BitAppPool);
            }
        }

        public string TargetRuntime
        {
            get
            {
                if (_targetRuntime.SelectedItem != null)
                    return (_targetRuntime.SelectedItem as ComboBoxItem).Tag as string;

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
            get 
            {
                var dic = new Dictionary<string, string>();
                foreach(var setting in this._appSettings.Settings)
                {
                    if (string.IsNullOrEmpty(setting.Value))
                        continue;

                    dic[setting.Key] = setting.Value;
                }
                return dic; 
            }
            set
            {
                this._appSettings.Settings.Clear();

                foreach(var kvp in value)
                {
                    var setting = new AppSettingsControl.AppSetting { Key = kvp.Key, Value = kvp.Value };
                    this._appSettings.Settings.Add(setting);
                }
            }
        }

        private string _healthCheckUri;
        public string HealthCheckUri 
        {
            get { return _healthCheckUri; }
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
            get { return _deploymentVersionLabel; }
            set
            {
                _deploymentVersionLabel = value;
                NotifyPropertyChanged(uiProperty_DeploymentVersionLabel);
            }
        }


        public void SetDefaultAppPoolFramework(string targetRuntime)
        {
            ComboBoxItem runtimeToSelect = null;
            if (!string.IsNullOrEmpty(targetRuntime))
            {
                foreach (ComboBoxItem cbi in _targetRuntime.Items)
                {
                    if ((cbi.Tag as string) == targetRuntime)
                    {
                        runtimeToSelect = cbi;
                        break;
                    }
                }
            }

            if (runtimeToSelect != null)
                _targetRuntime.SelectedItem = runtimeToSelect;
            else
                _targetRuntime.SelectedIndex = _targetRuntime.Items.Count - 1;
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
    }
}
