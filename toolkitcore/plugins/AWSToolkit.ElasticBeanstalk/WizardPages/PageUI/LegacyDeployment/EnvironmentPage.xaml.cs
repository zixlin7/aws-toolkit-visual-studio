using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment
{
    /// <summary>
    /// Interaction logic for EnvironmentPage.xaml
    /// </summary>
    internal partial class EnvironmentPage : INotifyPropertyChanged
    {
        public EnvironmentPage()
        {
            InitializeComponent();
        }

        public EnvironmentPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public bool CreateNewEnvironment => this._btnCreateNewEnv.IsChecked == true;

        public bool UserCreatingNewApp
        {
            set
            {
                _btnUseExistingEnv.IsEnabled = !value;

                // if this is a first time activation and the user has elected to update an
                // existing app, default to re-use of an environment
                if (!value)
                {
                    if (!PageController.HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv))
                        _btnUseExistingEnv.IsChecked = true;
                }
                else
                    _btnCreateNewEnv.IsChecked = true;
            }
        }

        /// <summary>
        /// Returns the type of the environment as specified by the user during new deployment,
        /// or optionally set for re-deployment if the user wants to change it.
        /// </summary>
        public string EnvironmentType
        {
            get
            {
                if (CreateNewEnvironment)
                    return (this._environmentType.SelectedItem as ComboBoxItem).Tag as string;

                return null;
            }
        }

        public IEnumerable<EnvironmentDescription> ExistingEnvironments
        {
            set
            {
                this._existingEnvs.ItemsSource = value;
                if (value == null)
                {
                    this._existingEnvs.Cursor = Cursors.Wait;
                    return;
                }

                this._existingEnvs.Cursor = Cursors.Arrow;
                bool revertToCreateNew = false;

                EnvironmentDescription selectedEnv = null;
                if (value.Count<EnvironmentDescription>() > 0)
                {
                    // property only set if we're redeploying an app; if we can't find it then switch back to create-new mode
                    string lastEnvName = PageController.HostingWizard[BeanstalkDeploymentWizardProperties.SeedData.propkey_SeedEnvName] as string;
                    if (!string.IsNullOrEmpty(lastEnvName))
                    {
                        foreach (EnvironmentDescription env in value)
                        {
                            if (string.Compare(env.EnvironmentName, lastEnvName, StringComparison.CurrentCultureIgnoreCase) == 0)
                            {
                                selectedEnv = env;
                                break;
                            }
                        }
                    }
                }
                else
                    revertToCreateNew = true;

                if (revertToCreateNew)
                    this._btnCreateNewEnv.IsChecked = true;
                else
                {
                    if (selectedEnv != null)
                        this._existingEnvs.SelectedItem = selectedEnv;
                    else
                        this._existingEnvs.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Returns either the new env name or the name of an existing
        /// env to be used
        /// </summary>
        public string EnvironmentName
        {
            get
            {
                if (CreateNewEnvironment)
                    return this._envName.Text.Trim();
                else
                {
                    if (this._existingEnvs.SelectedItem != null)
                        return (this._existingEnvs.SelectedItem as EnvironmentDescription).EnvironmentName;
                    else
                        return string.Empty;
                }
            }
        }

        public string EnvironmentDescription => this._envDescription.Text;

        public string CName => this._cname.Text.Trim();

        public bool NewEnvironmentNameIsValid => ValidateEnteredEnvironmentName();

        public bool CNameIsValid
        {
            get 
            {
                if (!IsInitialized)
                    return false;

                return string.IsNullOrEmpty(ValidateCNameContent(_cname.Text)); 
            }
        }

        public bool SelectedExistingEnvironmentIsValid
        {
            get
            {
                EnvironmentDescription env = this._existingEnvs.SelectedItem as EnvironmentDescription;
                if (env != null)
                {
                    if (env.Status == BeanstalkConstants.STATUS_READY)
                        return true;
                }

                return false;                            
            }
        }

        private void _btnUseExistingEnv_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            SetPageForEnvironmentReuse();
            NotifyPropertyChanged("useExistingEnvironment");
        }

        private void _btnCreateNewEnv_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            SetPageForNewEnvironmentEntry();
            NotifyPropertyChanged("createNewEnvironment");
        }

        bool IsSingleInstanceSelected(ComboBox field)
        {
            if (field.SelectedItem != null)
                return (((field.SelectedItem as ComboBoxItem).Tag as string).Equals(BeanstalkConstants.EnvType_SingleInstance, 
                                                                                    StringComparison.Ordinal));

            return false;
        }

        private void SetPageForNewEnvironmentEntry()
        {
            this._cname.IsEnabled = true;
            this._btnCheckAvailability.IsEnabled = !string.IsNullOrEmpty(this._cname.Text);

            ValidateEnteredEnvironmentName();
            this._envNameValidationMsg.Visibility = Visibility.Visible;
            this._envReadinessMsg.Visibility = Visibility.Hidden;
        }

        private void SetPageForEnvironmentReuse()
        {
            this._cname.IsEnabled = false;
            this._btnCheckAvailability.IsEnabled = false;

            ValidateEnvironmentReadiness(this._existingEnvs.SelectedItem as EnvironmentDescription);
            this._envNameValidationMsg.Visibility = Visibility.Hidden;
            this._envReadinessMsg.Visibility = Visibility.Visible;
        }

        private void _btnCheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            bool validatedOK = true;

            try
            {
                AccountViewModel selectedAccount = PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                RegionEndPointsManager.RegionEndPoints region 
                        = PageController.HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
                var beanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(selectedAccount, region);
                CheckDNSAvailabilityResponse response = beanstalkClient.CheckDNSAvailability(new CheckDNSAvailabilityRequest() { CNAMEPrefix = this._cname.Text.Trim() });
                if (response.Available)
                    _urlValidatedMsg.Text = "The requested URL is available";
                else
                {
                    _urlValidatedMsg.Text = "The requested URL is not available";
                    validatedOK = false;
                }
            }
            catch (Exception exc)
            {
                PageController.HostingWizard.Logger.InfoFormat("Exception during cname availability check, {0}", exc.Message);
                _urlValidatedMsg.Text = "Error during URL validation; check URL and try again";
                validatedOK = false;
            }
            finally
            {
                _urlValidatedMsg.Visibility = Visibility.Visible;
                if (validatedOK)
                {
                    _urlValidatedMsg.Foreground = Brushes.Green;
                    _validateOKImg.Visibility = Visibility.Visible;
                    _validateFailImg.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _urlValidatedMsg.Foreground = Brushes.Red;
                    _validateOKImg.Visibility = Visibility.Collapsed;
                    _validateFailImg.Visibility = Visibility.Visible;
                }
                // disable the check button until the use changes the text again
                _btnCheckAvailability.IsEnabled = false;
                Cursor = Cursors.Arrow;
            }
        }

        private void _cname_TextChanged(object sender, TextChangedEventArgs e)
        {
            string contentValidation = ValidateCNameContent(this._cname.Text);
            
            if (string.IsNullOrEmpty(contentValidation))
            {
                this._btnCheckAvailability.IsEnabled = !string.IsNullOrEmpty(this._cname.Text);
                _urlValidatedMsg.Visibility = Visibility.Hidden;
            }
            else
            {
                this._btnCheckAvailability.IsEnabled = false;

                _urlValidatedMsg.Text = contentValidation;
                _urlValidatedMsg.Foreground = Brushes.Red;
                _urlValidatedMsg.Visibility = Visibility.Visible;
                _validateFailImg.Visibility = Visibility.Visible;
            }

            _validateFailImg.Visibility = Visibility.Collapsed;
            _validateOKImg.Visibility = Visibility.Collapsed;
            
            NotifyPropertyChanged("CName");
        }

        string ValidateCNameContent(string cname)
        {
            if (!string.IsNullOrEmpty(cname))
            {
                if (!Uri.IsWellFormedUriString(string.Format("http://{0}.elasticbeanstalk.com", cname), UriKind.Absolute))
                    return "Invalid URL";
            }

            return string.Empty;
        }

        bool ValidateEnteredEnvironmentName()
        {
            string validationMsg = null;
            if (_envName.Text.Length < 4)
                validationMsg = "Minimum 4 characters";
            else
            {
                // according to docs http://docs.amazonwebservices.com/elasticbeanstalk/latest/api
                string envName = _envName.Text;
                bool isValid = envName[0] != '-'
                                && envName[envName.Length - 1] != '-';
                if (isValid)
                {
                    // perform deeper validation that content is only alphabetic, numeric or hyphen
                    foreach (char c in envName)
                    {
                        if (char.IsLetterOrDigit(c) || c == '-')
                            continue;
                        else
                        {
                            isValid = false;
                            break;
                        }
                    }
                }

                if (!isValid)
                    validationMsg = "Letters, digits or hyphens only; no spaces or leading/trailing hyphens";
            }

            _envNameValidationMsg.Text = validationMsg;
            return string.IsNullOrEmpty(validationMsg);
        }

        void ValidateEnvironmentReadiness(EnvironmentDescription env)
        {
            if (env != null)
            {
                if (env.Status == BeanstalkConstants.STATUS_READY)
                {
                    _envReadinessMsg.Text = "Environment is available for redeployment";
                    _envReadinessMsg.Foreground = Brushes.Green;
                }
                else
                {
                    _envReadinessMsg.Text = "Environment must be at 'Ready' state before redeployment can proceed";
                    _envReadinessMsg.Foreground = Brushes.Red;
                }
            }
            else
                _envReadinessMsg.Text = string.Empty;
        }

        void OnEnvNameChanged(object sender, RoutedEventArgs e)
        {
            ValidateEnteredEnvironmentName();
            NotifyPropertyChanged("envName");
        }

        // mimic console and seed cname from initial environment name
        void _envName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this._cname.Text) && NewEnvironmentNameIsValid)
                this._cname.Text = this._envName.Text.Trim();
        }

        void _existingEnvs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnvironmentDescription env = this._existingEnvs.SelectedItem as EnvironmentDescription;
            ValidateEnvironmentReadiness(env);
            NotifyPropertyChanged("ExistingEnvironment");
        }

        void RefreshEnvironmentsButton_Click(object sender, RoutedEventArgs e)
        {
            _envReadinessMsg.Text = string.Empty;
            NotifyPropertyChanged("RefreshEnvironments");
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (this._btnCreateNewEnv.IsChecked == true)
                SetPageForNewEnvironmentEntry();
            else
                SetPageForEnvironmentReuse();
        }
    }
}
