using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using System.Text.RegularExpressions;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI
{
    /// <summary>
    /// Interaction logic for CommonAppOptionsPage.xaml
    /// </summary>
    public partial class CommonAppOptionsPage : INotifyPropertyChanged
    {
        readonly object _syncLock = new object();

        // see http://haacked.com/archive/2007/08/21/i-knew-how-to-validate-an-email-address-until-i.aspx
        const string EmailValidationPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                                              + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                                              + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";
        static readonly Regex _emailValidationRegex = new Regex(EmailValidationPattern, RegexOptions.IgnoreCase);

        public static readonly string uiProperty_HealthCheckUrl = "healthCheckUrl";
        public static readonly string uiProperty_CredentialsMode = "credentialsMode";
        public static readonly string uiProperty_Credentials = "credentials"; // covers access key, secret key and iam user selection

        public static readonly string APPPARAM1_KEY = "PARAM1";
        public static readonly string APPPARAM2_KEY = "PARAM2";
        public static readonly string APPPARAM3_KEY = "PARAM3";
        public static readonly string APPPARAM4_KEY = "PARAM4";
        public static readonly string APPPARAM5_KEY = "PARAM5";

        public enum AppCredentials
        {
            UseNone,
            ReuseLast,
            UseSpecified,
            UseDeploymentAccount,
            UseIAM
        }

        AdornerLayer _pageRootAdornerLayer;
        LoadingMessageAdorner _loadingMessageAdorner;

        bool _useEC2InstanceStatusForHealthChecks = false;

        class IAMUserWrapper
        {
            public IAMUserWrapper(string userName, string accessKey)
            {
                this.UserName = userName;
                this.AccessKey = accessKey;
            }

            public string UserName { get; private set; }
            public string AccessKey { get; private set; }
        }

        public CommonAppOptionsPage()
        {
            InitializeComponent();
        }

        public CommonAppOptionsPage(IAWSWizardPageController pageController)
            : this()
        {
            PageController = pageController;

            if (pageController.HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_ShowV2RuntimeOnly)
                    && (bool)pageController.HostingWizard[DeploymentWizardProperties.AppOptions.propkey_ShowV2RuntimeOnly])
            {
                // can't modify collection whilst enumerating
                var toRemove = new List<ComboBoxItem>();
                foreach (ComboBoxItem cbi in _targetFramework.Items)
                {
                    if ((cbi.Tag as string).StartsWith("4"))
                    {
                        toRemove.Add(cbi);
                    }
                }

                foreach (ComboBoxItem cbi in toRemove)
                {
                    _targetFramework.Items.Remove(cbi);
                }
            }
        }

        public IAWSWizardPageController PageController { get; set; }

        public string TargetFramework
        {
            get
            {
                if (_targetFramework.SelectedItem != null)
                    return (_targetFramework.SelectedItem as ComboBoxItem).Tag as string;
                else
                    return string.Empty;
            }
        }

        public bool Enable32BitApplications
        {
            get { return _enable32BitApplications.IsChecked == true; }
        }

        public void SetAppPoolSettings(string targetFramework, bool enable32BitApps)
        {
            ComboBoxItem frameworkToSelect = null;
            if (!string.IsNullOrEmpty(targetFramework))
            {
                foreach (ComboBoxItem cbi in _targetFramework.Items)
                {
                    if ((cbi.Tag as string) == targetFramework)
                    {
                        frameworkToSelect = cbi;
                        break;
                    }
                }
            }

            if (frameworkToSelect != null)
                _targetFramework.SelectedItem = frameworkToSelect;
            else
                _targetFramework.SelectedIndex = _targetFramework.Items.Count - 1;

            _enable32BitApplications.IsChecked = enable32BitApps;
        }

        public bool UseEC2InstanceStatusForHealthChecks
        {
            set
            {
                _useEC2InstanceStatusForHealthChecks = value;

                this._healthCheckViaInstanceStatusPanel.Visibility = _useEC2InstanceStatusForHealthChecks ? Visibility.Visible : Visibility.Collapsed;
                this._healthCheckViaURLPanel.Visibility = _useEC2InstanceStatusForHealthChecks ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public string HealthCheckURL
        {
            get { return _healthCheckURL.Text.Trim(); }
        }

        public bool HealthCheckUrlIsValid
        {
            get
            {
                if (!IsInitialized || _useEC2InstanceStatusForHealthChecks)
                    return true;

                if (!string.IsNullOrEmpty(this._healthCheckURL.Text))
                    return Uri.IsWellFormedUriString(string.Format("http://test.elasticbeanstalk.com{0}",
                                                                   this._healthCheckURL.Text),
                                                     UriKind.Absolute);

                return false;
            }
        }

        public string NotificationEmail
        {
            get 
            {
                if (this._emailControlsGrid.Visibility == Visibility.Visible)
                    return this._notificationEmail.Text.Trim();
                else
                    return string.Empty;
            }
        }

        public bool NotificationEmailIsValid
        {
            get
            {
                if (!IsInitialized)
                    return true;

                if (this._emailControlsGrid.Visibility == Visibility.Visible)
                {
                    if (string.IsNullOrEmpty(this._notificationEmail.Text))
                        return true;

                    return IsValidEmail(this._notificationEmail.Text);
                }
                else
                    return true;
            }
        }

        public string GetAppParam(string appParamKey)
        {
            if (appParamKey == APPPARAM1_KEY)
                return this._param1.Text;

            if (appParamKey == APPPARAM2_KEY)
                return this._param2.Text;

            if (appParamKey == APPPARAM3_KEY)
                return this._param3.Text;

            if (appParamKey == APPPARAM4_KEY)
                return this._param4.Text;

            if (appParamKey == APPPARAM5_KEY)
                return this._param5.Text;

            throw new ArgumentException("Unknown app parameter key - " + appParamKey);
        }

        public string AccessKey
        {
            get { return _accessKey.Text; }
        }

        public string SecretKey
        {
            get { return _secretKey.Text; }
        }

        public string SelectedAccountName
        {
            set
            {
                btnUseMyCredentials.Content = string.Format("_Use credentials from profile '{0}'", value);
            }
        }

        public bool IsRedeploying 
        {
            set
            {
                this.btnReUseCredentials.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void SetAppParams(Dictionary<string, string> appParams, 
                                 string healthCheckUrl, 
                                 string notificationEmail,
                                 bool isRedeploying)
        {
            IsRedeploying = isRedeploying;
            if (isRedeploying)
            {
                this.btnUseNoCredentials.Visibility = Visibility.Collapsed;
                this.btnReUseCredentials.Visibility = Visibility.Visible;
                this.btnReUseCredentials.IsChecked = true;
            }
            else
            {
                this.btnUseNoCredentials.Visibility = Visibility.Visible;
                this.btnReUseCredentials.Visibility = Visibility.Collapsed;
                this.btnUseNoCredentials.IsChecked = true;
                this._accessKey.Text = string.Empty;
                this._secretKey.Text = string.Empty;
            }

            SetParamField(appParams, APPPARAM1_KEY, this._param1);
            SetParamField(appParams, APPPARAM2_KEY, this._param2);
            SetParamField(appParams, APPPARAM3_KEY, this._param3);
            SetParamField(appParams, APPPARAM4_KEY, this._param4);
            SetParamField(appParams, APPPARAM5_KEY, this._param5);

            if (!string.IsNullOrEmpty(healthCheckUrl))
                _healthCheckURL.Text = healthCheckUrl;

            if (!string.IsNullOrEmpty(notificationEmail))
                _notificationEmail.Text = notificationEmail;
        }

        public AppCredentials AppCredentialsMode
        {
            get
            {
                if (btnUseNoCredentials.IsChecked == true)
                    return AppCredentials.UseNone;

                if (btnReUseCredentials.IsChecked == true)
                    return AppCredentials.ReuseLast;

                if (btnUseSpecifiedCredentials.IsChecked == true)
                    return AppCredentials.UseSpecified;

                if (btnUseMyCredentials.IsChecked == true)
                    return AppCredentials.UseDeploymentAccount;

                return AppCredentials.UseIAM;
            }
        }

        public string SelectedIAMUserKey
        {
            get
            {
                IAMUserWrapper userWrapper = this.iamUserList.SelectedItem as IAMUserWrapper;
                if (userWrapper != null)
                    return userWrapper.AccessKey;

                return string.Empty;
            }
        }

        bool _dataLoadPending = false;
        public bool DataLoadPending
        {
            get
            {
                bool ret;
                lock (_syncLock)
                    ret = _dataLoadPending;
                return ret;
            }
            set
            {
                lock (_syncLock)
                {
                    _dataLoadPending = value;
                    if (_dataLoadPending)
                    {
                        if (_pageRootAdornerLayer == null)
                        {
                            _pageRootAdornerLayer = AdornerLayer.GetAdornerLayer(_pageRoot);
                            if (_pageRootAdornerLayer == null)
                                return;
                        }

                        if (_loadingMessageAdorner == null)
                            _loadingMessageAdorner = new LoadingMessageAdorner(_pageRoot, "Querying prior deployment information...");

                        _pageRootAdornerLayer.Add(_loadingMessageAdorner);
                    }
                    else
                    {
                        if (_pageRootAdornerLayer != null)
                            _pageRootAdornerLayer.Remove(_loadingMessageAdorner);
                    }
                }
            }
        }

        /// <summary>
        /// Used to set the enablement of the 'use an IAM user' radio button'; we don't
        /// populate the dropdown until the user checks the button
        /// </summary>
        public bool IAMSelectionAvailable
        {
            set
            {
                btnUseIAMUserCredentials.IsEnabled = value;
            }
        }

        public Dictionary<string, List<string>> IAMUserAccounts
        {
            set
            {
                // was thinking of showing a tree in the dropdown but for now, flatten to a grid
                // display
                List<IAMUserWrapper> userWrappers = new List<IAMUserWrapper>();
                foreach (string userName in value.Keys)
                {
                    List<string> accessKeys = value[userName];
                    foreach (string accessKey in accessKeys)
                    {
                        userWrappers.Add(new IAMUserWrapper(userName, accessKey));
                    }
                }

                iamUserList.ItemsSource = userWrappers;
                iamUserList.Cursor = Cursors.Arrow;
            }
        }

        public bool EmailControlsVisible
        {
            set
            {
                this._emailControlsGrid.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void SetParamField(Dictionary<string, string> appParams, string paramKey, TextBox paramField)
        {
            if (appParams != null && appParams.ContainsKey(paramKey))
                paramField.Text = appParams[paramKey];
            else
                paramField.Text = string.Empty;
        }

        private void CredentialsModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnUseIAMUserCredentials)
            {
                iamUserList.ItemsSource = null;
                iamUserList.Cursor = Cursors.Wait;
            }

            NotifyPropertyChanged(uiProperty_CredentialsMode);
        }

        private void iamUserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Credentials);
        }

        private void CredentialsTextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Credentials);
        }

        private void _healthCheckURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized)
            {
                this._healthcheckValidationFailIcon.Visibility = HealthCheckUrlIsValid ? Visibility.Collapsed : Visibility.Visible;
                NotifyPropertyChanged(uiProperty_HealthCheckUrl);
            }
        }

        private void _notificationEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsInitialized)
            {
                // variations in how the server interprets the local part of the address can make it
                // hard to precisely validate, so offer a 'guide' instead...
                // see http://haacked.com/archive/2007/08/21/i-knew-how-to-validate-an-email-address-until-i.aspx
                this._emailValidationWarning.Visibility = NotificationEmailIsValid ? Visibility.Collapsed : Visibility.Visible;
                NotifyPropertyChanged("email");
            }
        }

        bool IsValidEmail(string emailAddress)
        {
            return _emailValidationRegex.IsMatch(emailAddress);
        }

        private void _targetFramework_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // we don't know the actual template if we're redeploying and don't have a means to
            // discover the setting yet, so skip warning on redeploy for now
            bool isRedeploying = false;
            if (PageController.HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                isRedeploying = (bool)PageController.HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
            if (isRedeploying)
                return;

            // strobe temp fix: if redeploy but to new environment, we don't have a template set
            if (PageController.HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate))
            {
                string version = (_targetFramework.SelectedItem as ComboBoxItem).Tag as string;
                var template = PageController.HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate]
                                    as DeploymentTemplateWrapperBase;
                _targetFrameworkWarnIcon.Visibility = template.SupportsFrameworkVersion(version) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

    }
}
