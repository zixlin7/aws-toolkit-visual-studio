using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for ApplicationPage.xaml
    /// </summary>
    public partial class ApplicationPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_ApplicationName = "ApplicationName";
        public static readonly string uiProperty_EnvironmentName = "EnvironmentName";
        public static readonly string uiProperty_EnvironmentNames = "EnvironmentNames";
        public static readonly string uiProperty_CName = "CName";

        public static readonly string[] EnvironmentNamePresetSuffixes = {"-dev", "-test", "-prod"};

        public ApplicationPage()
        {
            InitializeComponent();
            DataContext = this;           
        }

        public ApplicationPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public bool HasErrors => !IsValid(this);

        public void ConfigureForAppVersionRedeployment()
        {
            _applicationName.IsEnabled = false;
        }

        private bool IsValid(DependencyObject obj)
        {
            // The dependency object is valid if it has no errors and all
            // of its children (that are dependency objects) are error-free.
            return !Validation.GetHasError(obj) &&
                LogicalTreeHelper.GetChildren(obj)
                .OfType<DependencyObject>()
                .All(IsValid);
        }

        public IAWSWizardPageController PageController { get; set; }

        private readonly ObservableCollection<string> _deployedApplicationNames = new ObservableCollection<string>();

        public ObservableCollection<string> DeployedApplicationNames => _deployedApplicationNames;

        public void SetAvailableApplicationDeployments(ICollection<DeployedApplicationModel> deployments)
        {
            DeployedApplicationNames.Clear();
            if (deployments != null)
            {
                foreach (var d in deployments)
                {
                    DeployedApplicationNames.Add(d.ApplicationName);
                }
            }
        }

        string _applicationNameValue = string.Empty;
        public string ApplicationName
        {
            get => this._applicationNameValue;
            set
            {
                _applicationNameValue = value;

                NotifyPropertyChanged(uiProperty_ApplicationName);
                ResetEnvironmentNamePresets();
            }
        }

        public bool IsApplicationNameValid
        {
            get
            {
                if (string.IsNullOrEmpty(ApplicationName) || ApplicationName.Length > 100)
                {
                    return false;
                }

                return true;
            }
        }

        private readonly ObservableCollection<string> _environmentNamePresets = new ObservableCollection<string>();

        public ObservableCollection<string> EnvironmentNamePresets => _environmentNamePresets;

        string _envNameValue;
        public string EnvironmentName
        {
            get => _envNameValue;
            set 
            {
                if (this.CName == null || string.Equals(this.CName, TransformToCName(this._envNameValue), StringComparison.InvariantCultureIgnoreCase))
                    this.CName = TransformToCName(value);

                this._envNameValue = value;

                NotifyPropertyChanged(uiProperty_EnvironmentName);
            }
        }

        public bool IsEnvironmentNameValid
        {
            get
            {
                if (EnvironmentName == null || EnvironmentName.Length < 4 || EnvironmentName.Length > 40)
                {
                    return false;
                }

                if (EnvironmentName[0] == '-')
                    return false;

                if (EnvironmentName[EnvironmentName.Length - 1] == '-')
                    return false;

                foreach (var c in EnvironmentName)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-')
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private string TransformToCName(string name)
        {
            if(name == null)
                return string.Empty;

            name = name.ToLowerInvariant();
            var sb = new StringBuilder();
            foreach(var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '-')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private string _cnameValue;
        public string CName
        {
            get => _cnameValue;
            set
            {
                this._cnameValue = value;
                UpdateCheckAvailabilityState();
                NotifyPropertyChanged(uiProperty_CName);
            }
        }

        public bool IsCNameValid => ValidateCNameContent(CName) == string.Empty;

        public bool CheckCNAMEAvailability()
        {
            Cursor = Cursors.Wait;
            bool validatedOK = true;

            try
            {
                var selectedAccount = PageController.HostingWizard.GetSelectedAccount();
                var region = PageController.HostingWizard.GetSelectedRegion();
                var beanstalkClient = selectedAccount.CreateServiceClient<AmazonElasticBeanstalkClient>(region);
                var response = beanstalkClient.CheckDNSAvailability(new CheckDNSAvailabilityRequest { CNAMEPrefix = this.CName.Trim() });
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
                // disable the check button until the user changes the text again
                _btnCheckAvailability.IsEnabled = false;
                Cursor = Cursors.Arrow;
            }

            return validatedOK;
        }

        private void _btnCheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            CheckCNAMEAvailability();
        }

        private void UpdateCheckAvailabilityState()
        {
            string contentValidation = ValidateCNameContent(this.CName);

            if (string.IsNullOrEmpty(contentValidation))
            {
                this._btnCheckAvailability.IsEnabled = !string.IsNullOrEmpty(this.CName);
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

        private void ResetEnvironmentNamePresets()
        {
            _environmentNamePresets.Clear();

            var appName = ApplicationName;
            if (!string.IsNullOrEmpty(appName))
            {
                foreach (var suffix in EnvironmentNamePresetSuffixes)
                {
                    _environmentNamePresets.Add(appName + suffix);
                }
            }
            NotifyPropertyChanged(uiProperty_EnvironmentNames);
        }
    }
}
