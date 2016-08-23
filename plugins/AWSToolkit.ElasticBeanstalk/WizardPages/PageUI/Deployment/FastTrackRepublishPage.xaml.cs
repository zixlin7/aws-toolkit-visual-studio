using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.ComponentModel;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.Model;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for FastTrackRepublishPage.xaml
    /// </summary>
    public partial class FastTrackRepublishPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_VersionLabel = "DeploymentVersionLabel";

        readonly object _syncObj = new object();
        IEnumerable<ApplicationVersionDescription> _existingVersions = null;
        bool _usingIncrementalDeployment = true;

        public FastTrackRepublishPage()
        {
            InitializeComponent();
            DataContext = this;
			CoreCLRVisible = Visibility.Collapsed;
        }

        public FastTrackRepublishPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

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

        public Visibility CoreCLRVisible { get; set; }

        public ObservableCollection<string> BuildConfigurations { get; set; }

        public string SelectedBuildConfiguration { get; set; }

        public bool IsSelectedVersionValid
        {
            get
            {
                return ValidateVersionLabelField();
            }
        }

        public string DeploymentVersionLabel { get; set; }

        public string IISAppPath { get; set; }

        public string TargetFramework { get; set; }

        public void SetRedeploymentMessaging(AccountViewModel account, 
                                             RegionEndPointsManager.RegionEndPoints region, 
                                             EnvironmentDescription envDescription)
        {
            _envDetailsPanel.Children.Clear();

            AddToEnvironmentDetailsPanel(string.Format("Republish to environment '{0}' belonging to application '{1}'.",
                                                       envDescription.EnvironmentName,
                                                       envDescription.ApplicationName));

            AddToEnvironmentDetailsPanel(string.Format("This environment was created on {0} and last updated on {1}",
                                                       envDescription.DateCreated,
                                                       envDescription.DateUpdated));

            AddToEnvironmentDetailsPanel(string.Format("The environment CNAME is '{0}'", envDescription.CNAME));

            AddToEnvironmentDetailsPanel(string.Format("The credentials associated with account '{0}' will be used for the deployment.",
                                                        account.AccountDisplayName));

            this.VersionFetchPending = true;
            new QueryExistingApplicationVersionsWorker(account,
                                                       region,
                                                       envDescription.ApplicationName,
                                                       PageController.HostingWizard.Logger,
                                                       new QueryExistingApplicationVersionsWorker.DataAvailableCallback(OnVersionsAvailable));

        }

        public void SetDeploymentVersionLabelInfo(string versionLabel, bool usingIncrementalDeployment)
        {
            DeploymentVersionLabel = versionLabel;
            _usingIncrementalDeployment = usingIncrementalDeployment;
            ToggleCustomVersionLabelFields();
        }

        // shared handler for text fields that get changed; forward to validator if set so
        // Next button can be enabled appropriately for mandatory fields
        void OnVersionFieldChanged(object sender, RoutedEventArgs e)
        {
            if (sender == _versionLabel)
            {
                ValidateVersionLabelField();
                NotifyPropertyChanged(uiProperty_VersionLabel);
            }
        }

        void OnVersionsAvailable(IEnumerable<ApplicationVersionDescription> existingVersions)
        {
            this._existingVersions = existingVersions;
            VersionFetchPending = false;
            NotifyPropertyChanged(uiProperty_VersionLabel);
        }

        // wanted to use ValidationRules for this but can't find a way to get
        // the list of existing versions (or a reference to this page) through
        // the framework :-(
        bool ValidateVersionLabelField()
        {
            if (!IsInitialized)
                return true;

            if (VersionFetchPending)  // don't want a message at this stage
                return false;

            // version not used here
            if (_usingIncrementalDeployment)
                return true;

            bool isValid = false;

            string validationFailMsg = string.Empty;
            string version = DeploymentVersionLabel;
            if (!string.IsNullOrEmpty(version))
            {
                if (_existingVersions != null && _existingVersions.Count<ApplicationVersionDescription>() > 0)
                {
                    isValid = _existingVersions
                                    .Where<ApplicationVersionDescription>
                                            ((V) => { return string.Compare(V.VersionLabel, version, true) == 0; })
                                                    .Count<ApplicationVersionDescription>() == 0;
                }
                else
                    isValid = true;
                if (!isValid)
                    validationFailMsg = "Version label is already in use";
            }
            else
                validationFailMsg = "Version label may not be empty";

            _validateFailImg.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            _validateOKImg.Visibility = isValid ? Visibility.Visible : Visibility.Collapsed;
            _versionValidatedMsg.Text = isValid ? "Version label is available" : validationFailMsg;
            _versionValidatedMsg.Visibility = Visibility.Visible;

            return isValid;
        }

        void AddToEnvironmentDetailsPanel(string text)
        {
            // cannot get the assigned style to give us white-on-dark text from style when dark theme
            // is active, so force the issue by setting an explicit foreground too
            var tb = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(4),
                Text = text,
                Style = FindResource("awsTextBlockBaseStyle") as Style,
                Foreground = FindResource("awsDefaultControlForegroundBrushKey") as SolidColorBrush
            };

            _envDetailsPanel.Children.Add(tb);
        }

        void ToggleCustomVersionLabelFields()
        {
            this._versionLabel.IsEnabled = !_usingIncrementalDeployment;
            this._versionValidationGroup.Visibility = _usingIncrementalDeployment ? Visibility.Collapsed : Visibility.Visible;
        }

        public void SetDefaultRuntimesOrFrameworks(string targetFramework, IDictionary<string, string> availableFrameworks)
        {
            foreach (var key in availableFrameworks.Keys)
            {
                _targetFramework.Items.Add(key);
            }

            if (targetFramework != null)
                this.TargetFramework = targetFramework;
            else if(_targetFramework.Items.Count > 0)
                this.TargetFramework = _targetFramework.Items[0] as string ;
        }
    }
}
