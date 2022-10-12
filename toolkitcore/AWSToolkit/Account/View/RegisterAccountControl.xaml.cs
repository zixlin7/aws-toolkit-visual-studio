using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Account.View
{
    /// <summary>
    /// Interaction logic for RegisterAccountControl.xaml
    /// </summary>
    public partial class RegisterAccountControl : BaseAWSControl
    {
        private readonly RegisterAccountController _controller;

        public RegisterAccountControl(RegisterAccountController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            this._controller.Model.PropertyChanged += OnPropertyChanged;
            InitializeComponent();

            Unloaded += OnUnloaded;
        }

        public override string Title
        {
            get
            {
                if (Guid.Empty == this._controller.Model.UniqueKey)
                    return "New Account Profile";
                else
                    return "Edit Profile";
            }
        }

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.ProfileName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Profile Name is a required field");
                return false;
            }
            if (string.IsNullOrEmpty(this._controller.Model.AccessKey))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Access Key is a required field");
                return false;
            }
            if (string.IsNullOrEmpty(this._controller.Model.SecretKey))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Secret Key is a required field");
                return false;
            }


            return true;
        }

        public override string MetricId => this.GetType().FullName;

        // used to make the explanatory label of why a profile named 'default'
        // is useful. Not needed if user already has 'default' set up.
        public bool PromptToUseDefaultName => _controller.PromptToUseDefaultName;

        public override bool OnCommit()
        {
            this._controller.Persist();
            return true;
        }

        void OnRequestRegions(object sender, RoutedEventArgs e)
        {
            this._controller.Model.IsPartitionEnabled = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            _controller.Model.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_controller.Model.AccessKey))
            {
                UpdatePassword(_ctlAccessKey, _controller.Model.AccessKey);
            }

            if (e.PropertyName == nameof(_controller.Model.SecretKey))
            {
                UpdatePassword(_ctlSecretKey, _controller.Model.SecretKey);
            }

            if (e.PropertyName == nameof(this._controller.Model.Partition))
            {
                OnPartitionChanged();
            }

        }

        /// <summary>
        /// Updates the password value if it is different.
        /// Avoid assigning the same value, because this resets the control's cursor position.
        /// </summary>
        private void UpdatePassword(PasswordBox passwordBox, string password)
        {
            if (passwordBox.Password != password)
            {
                passwordBox.Password = password;
            }
        }

        private void OnPartitionChanged()
        {
            if (this._controller.Model.Partition == null)
            {
                return;
            }
            this._controller.Model.ShowRegionsForPartition(this._controller.Model.Partition.Id);

            if (!this._controller.Model.Regions.Any())
            {
                return;
            }

            // When the Partition changes the list of Regions, the currently selected Region
            // is likely cleared (from databinding).
            // Make a reasonable region selection, if the currently selected region is not available.
            var defaultRegion = RegionEndpoint.USEast1;

            var selectedRegion = this._controller.Model.GetRegion(ToolkitSettings.Instance.LastSelectedRegion)??
                                 this._controller.Model.GetRegion(defaultRegion.SystemName) ??
                                 this._controller.Model.Regions.FirstOrDefault();

            this._controller.Model.Region = selectedRegion;
        }

        private void OnClickImportFromCSV(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                CheckPathExists = true,
                Title = "Import AWS Credentials from CSV File"
            };

            var result = dlg.ShowDialog();
            if (result.GetValueOrDefault())
            {
                _controller.Model.LoadAWSCredentialsFromCSV(dlg.FileName);
            }
        }

        private void CtlSecretKey_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            this._controller.Model.SecretKey = this._ctlSecretKey.Password;
        }

        private void CtlAccessKey_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            this._controller.Model.AccessKey = this._ctlAccessKey.Password;
        }

        public override IHelpHandler GetHelpHandler() => _controller.Model;
    }
}
