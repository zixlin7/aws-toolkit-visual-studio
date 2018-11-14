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

using Amazon.AWSToolkit.RDS.Model;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for LaunchDBInstanceDetailsPage.xaml
    /// </summary>
    public partial class LaunchDBInstanceDetailsPage : INotifyPropertyChanged
    {
        const string AllocatedStorageLabelFormat = "(Minimum: {0} GB, Maximum {1} GB)";
        const string PASSWORD_MISMATCH = "Does not match password";

        public DBEngineMeta EngineMeta { get; private set; }
        ObservableCollection<DBEngineVersionWrapper> _engineVersions = new ObservableCollection<DBEngineVersionWrapper>();

        public LaunchDBInstanceDetailsPage()
        {
            InitializeComponent();
            _dbEngineVersions.ItemsSource = _engineVersions;
        }

        public void RefreshContent()
        {
            // we're guaranteed there's at least one version in the collection
            DBEngineVersionWrapper wrapper = _engineVersions[0];

            _logo.Source = wrapper.EngineIcon;

            EngineMeta = RDSServiceMeta.Instance.MetaForEngine(wrapper.Title);

            if (EngineMeta != null)
            {
                // right now we only have one license per engine
                _licenseModel.Text = EngineMeta.SupportedLicenses.FirstOrDefault<string>();
                _instanceClass.ItemsSource = EngineMeta.SupportedInstanceClasses;
                if (EngineMeta.SupportedInstanceClasses.Count() == 1)
                    _instanceClass.SelectedItem = EngineMeta.SupportedInstanceClasses.FirstOrDefault<DBInstanceClass>();

                _lblStorageLimits.Text = string.Format(AllocatedStorageLabelFormat, EngineMeta.MinStorageAlloc, EngineMeta.MaxStorageAlloc);

                // if whatever is in the field, if anything, is below min or above max then auto-adjust
                if (Storage == -1)
                    _allocatedStorage.Text = EngineMeta.MinStorageAlloc.ToString();

                _txtInstanceIdentifier.MaxLength = EngineMeta.MaxDBInstanceIdentifierLength;
                _txtMasterUserName.MaxLength = EngineMeta.MaxMasterUserNameLength;
                _txtMasterUserPassword.MaxLength = EngineMeta.MaxMasterPwdNameLength;

                _btnMultiAZDeployment.IsChecked = EngineMeta.SupportsMultiAZ;
                _btnMultiAZDeployment.IsEnabled = EngineMeta.SupportsMultiAZ;
            }

            _passwordConfirmationMsg.Text = string.Empty;
            _passwordRequirementMsg.Text = string.Empty;
        }

        public string LicenseModel
        {
            get { return this._licenseModel.Text; }
        }

        public List<DBEngineVersionWrapper> EngineVersions
        {
            set
            {
                _engineVersions.Clear();
                foreach (DBEngineVersionWrapper version in value)
                {
                    _engineVersions.Add(version);
                }

                if (_engineVersions.Count == 1)
                    _dbEngineVersions.SelectedItem = _engineVersions[0];
            }
        }

        public DBEngineVersion SelectedVersion
        {
            get 
            {
                var wrapper = this._dbEngineVersions.SelectedItem as DBEngineVersionWrapper;
                if (wrapper != null)
                    return wrapper.EngineVersion;

                return null;
            }
        }

        public DBInstanceClass InstanceClass
        {
            get { return this._instanceClass.SelectedItem as DBInstanceClass; }
        }

        public bool IsMultiAZ
        {
            get { return this._btnMultiAZDeployment.IsChecked == true; }
        }

        public bool AutoUpgradeMinorVersions
        {
            get { return this._btnAutoUpgradeMinorVersions.IsChecked == true; }
        }

        public int Storage
        {
            get
            {
                int storage;
                if (!string.IsNullOrEmpty(_allocatedStorage.Text) 
                        && int.TryParse(_allocatedStorage.Text, out storage))
                {
                    if (EngineMeta != null && storage >= EngineMeta.MinStorageAlloc && storage < EngineMeta.MaxStorageAlloc)
                        return storage;
                }

                return -1;
            }
        }

        public string DBInstanceIdentifier
        {
            get { return _txtInstanceIdentifier.Text; }
        }

        public string ValidateDBInstanceIdentifier()
        {
            if (_txtInstanceIdentifier.Text.Length > 0)
            {
                string proposedIdentifier = _txtInstanceIdentifier.Text;
                if (!char.IsLetter(proposedIdentifier.ToCharArray()[0]))
                    return "Must begin with a letter";

                if (proposedIdentifier.EndsWith("-"))
                    return "Must not end with a hyphen";

                if (proposedIdentifier.IndexOf("--") != -1)
                    return "Must not contain two consecutive hyphens";

                // now do a general check that it contains only letters, 
                // digits or hyphens (this applies to all db engines)
                foreach (var c in _txtInstanceIdentifier.Text)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-')
                        return "Must contain only letters, digits or hyphens";
                }
            }

            return string.Empty;
        }

        public string MasterUserName
        {
            get { return _txtMasterUserName.Text; }
        }

        public string ValidateUserName()
        {
            if (_txtMasterUserName.Text.Length > 0)
            {
                string proposedName = _txtMasterUserName.Text;
                if (!char.IsLetter(proposedName.ToCharArray()[0]))
                    return "Must begin with a letter";
            }

            return string.Empty;
        }

        public string MasterUserPassword
        {
            get { return _txtMasterUserPassword.Password; }
        }

        public bool IsPasswordValid
        {
            get
            {
                return PasswordAndConfirmationMatch(_txtMasterUserPassword.Password, _txtPasswordConfirmation.Password) == true
                        && string.IsNullOrEmpty(PasswordMeetsEngineRequirements(_txtMasterUserPassword.Password));
            }
        }

        private void _dbEngineVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("engineVersion");
        }

        private void _instanceClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("instanceClass");
        }

        private void _allocatedStorage_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // may be paste of a string, so try and convert rather than use IsDigit
            int num;
            if (!int.TryParse(e.Text, out num))
                e.Handled = true;
        }

        private void _allocatedStorage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EngineMeta != null)
            {
                var fore = FindResource("awsDefaultControlForegroundBrushKey") as SolidColorBrush;
                int num;
                if (int.TryParse(_allocatedStorage.Text, out num))
                {
                    if (num < EngineMeta.MinStorageAlloc || num > EngineMeta.MaxStorageAlloc)
                        fore = Brushes.Red;
                }
                else
                    fore = Brushes.Red;
                _lblStorageLimits.Foreground = fore;
            }
            NotifyPropertyChanged("storage");
        }

        private void _txtInstanceIdentifier_TextChanged(object sender, TextChangedEventArgs e)
        {
            _instanceIdValidationMsg.Text = ValidateDBInstanceIdentifier();
            NotifyPropertyChanged("dbInstanceIdentifier");
        }

        private void _txtMasterUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            _userNameValidationMsg.Text = ValidateUserName();
            NotifyPropertyChanged("masterUserName");
        }

        private void OnPasswordFieldsChanged(object sender, RoutedEventArgs e)
        {
            bool? testResult = PasswordAndConfirmationMatch(_txtMasterUserPassword.Password, _txtPasswordConfirmation.Password);
            if (testResult == false)
            {
                _passwordRequirementMsg.Text = string.Empty;
                _passwordConfirmationMsg.Text = PASSWORD_MISMATCH;
            }
            else
            {
                if (!string.IsNullOrEmpty(_txtMasterUserPassword.Password))
                {
                    _passwordConfirmationMsg.Text = string.Empty;
                    _passwordRequirementMsg.Text = PasswordMeetsEngineRequirements(_txtMasterUserPassword.Password);
                }
                else
                {
                    _passwordConfirmationMsg.Text = string.Empty;
                    _passwordRequirementMsg.Text = string.Empty;
                }
            }

            NotifyPropertyChanged("password");
        }

        internal bool? PasswordAndConfirmationMatch(string password, string confirmation)
        {
            if (!string.IsNullOrEmpty(_txtMasterUserPassword.Password) && !string.IsNullOrEmpty(_txtPasswordConfirmation.Password))
                return string.Compare(_txtMasterUserPassword.Password, _txtPasswordConfirmation.Password, false) == 0;
            else
                return null;
        }

        private string PasswordMeetsEngineRequirements(string password)
        {
            string validationMsg = string.Empty;

            if (EngineMeta != null && (password.Length < EngineMeta.MinMasterPwdNameLength
                    || password.Length > EngineMeta.MaxMasterPwdNameLength))
                validationMsg = string.Format("Must contain {0} to {1} alphanumeric characters",
                                              EngineMeta.MinMasterPwdNameLength,
                                              EngineMeta.MaxMasterPwdNameLength);

            return validationMsg;
        }
    }
}
