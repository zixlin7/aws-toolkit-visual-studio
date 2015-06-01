using System;
using System.Collections.Generic;
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

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.Account.View;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment
{
    /// <summary>
    /// Interaction logic for ApplicationPage.xaml
    /// </summary>
    internal partial class ApplicationPage
    {
        string _lastSeenAccount = string.Empty;
        object _syncObj = new object();

        // this gets us round timing issue of binding account instance
        // into account selector's SelectedItem property but it not having 
        // caught up by time subsequent Click event fires after _useExisting.IsChecked
        // and we want account to get apps
        AccountViewModel _redeploymentAccount = null;

        IEnumerable<ApplicationVersionDescription> _existingVersions = null;

        public ApplicationPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ApplicationPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        // locked to us-east, seeded on page creation, for the moment
        public RegionEndPointsManager.RegionEndPoints SelectedRegion { get; set; }

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

        public bool IsSelectedVersionValid
        {
            get
            {
                return ValidateVersionLabelField();
            }
        }

        public void Initialize(string appName, string appDescription, string versionLabel, bool isRedeployment, bool? useIncremental)
        {
            this.AppName = appName;
            this.AppDescription = appDescription;
            this.DeploymentVersionLabel = versionLabel;
            IsRedeploying = isRedeployment;

            this._txtAppName.IsEnabled = !isRedeployment;
            this._txtAppDescription.IsEnabled = !isRedeployment;

            if (RegionSupportsIncrementalDeployment)
            {
                // if the flag is null, that means we've detected .Net 4.5 beta#1 and a shell of VS2010, which is incompatible with
                // our 3rd party libs
                if (useIncremental != null)
                {
                    this._btnIncrementalDeployment.IsChecked = useIncremental.GetValueOrDefault();
                    this._btnIncrementalDeployment.IsEnabled = true;
                }
                else
                {
                    this._btnIncrementalDeployment.IsChecked = false;
                    this._incDeploymentGroup.Visibility = Visibility.Collapsed;
                }
                ToggleCustomVersionLabelFields(!(this._btnIncrementalDeployment.IsChecked == true));
            }
            else
            {
                this._incDeploymentGroup.Visibility = Visibility.Collapsed;
                this._btnIncrementalDeployment.IsChecked = false;
                ToggleCustomVersionLabelFields(true);
            }

            if (isRedeployment)
            {
                AccountViewModel account
                        = PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;

                if (account != null)
                {
                    _redeploymentAccount = account;

                    VersionFetchPending = true;
                    new QueryExistingApplicationVersionsWorker(account,
                                                               PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                                                   as RegionEndPointsManager.RegionEndPoints,
                                                               appName,
                                                               PageController.HostingWizard.Logger,
                                                               new QueryExistingApplicationVersionsWorker.DataAvailableCallback(OnVersionsAvailable));

                    return;
                }
            }
        }

        bool IsRedeploying { get; set; }

        /// <summary>
        /// Returns either the new app name or the name of an existing
        /// app to be updated
        /// </summary>
        public string AppName { get; set; }

        public string AppDescription { get; set; }

        public string DeploymentVersionLabel { get; set; }

        public bool UseIncrementalDeployment
        {
            get 
            {
                if (!IsInitialized)
                    return false;

                return this._incDeploymentGroup.Visibility == Visibility.Visible 
                                && this._btnIncrementalDeployment.IsChecked == true; 
            }
        }

        void OnVersionsAvailable(IEnumerable<ApplicationVersionDescription> existingVersions)
        {
            this._existingVersions = existingVersions;
            VersionFetchPending = false;
            AlertControllerToFieldChange();
        }

        void AlertControllerToFieldChange()
        {
            if (PageController != null)
                PageController.TestForwardTransitionEnablement();
        }

        // shared handler for text fields that get changed; forward to validator if set so
        // Next button can be enabled appropriately for mandatory fields
        void OnTextFieldChanged(object sender, RoutedEventArgs e)
        {
            if (sender == _versionLabel)
                ValidateVersionLabelField();

            AlertControllerToFieldChange();
        }

        bool RegionSupportsIncrementalDeployment
        {
            get
            {
                return this.SelectedRegion.GetEndpoint(BeanstalkConstants.GIT_PUSH_SERVICE_NAME) != null;
            }
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
            if (this._incDeploymentGroup.Visibility == Visibility.Visible && this._btnIncrementalDeployment.IsChecked == true) 
                return true;

            bool isValid = false;

            string validationFailMsg = string.Empty;
            string version = DeploymentVersionLabel;
            if (!string.IsNullOrEmpty(version))
            {
                if (IsRedeploying)
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
                    isValid = true;
            }
            else
                validationFailMsg = "Version label may not be empty";

            _validateFailImg.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            _validateOKImg.Visibility = isValid ? Visibility.Visible : Visibility.Collapsed;
            _versionValidatedMsg.Text = isValid ? "Version label is available" : validationFailMsg;
            _versionValidatedMsg.Visibility = Visibility.Visible;

            return isValid;
        }

        private void _btnIncrementalDeployment_Click(object sender, RoutedEventArgs e)
        {
            ToggleCustomVersionLabelFields(!(this._btnIncrementalDeployment.IsChecked == true));
            AlertControllerToFieldChange();
        }

        void ToggleCustomVersionLabelFields(bool allowCustomVersion)
        {
            this._versionLabel.IsEnabled = allowCustomVersion;
            this._versionValidationGroup.Visibility = allowCustomVersion ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
