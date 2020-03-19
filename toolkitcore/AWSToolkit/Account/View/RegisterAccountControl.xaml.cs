using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Account.Controller;

namespace Amazon.AWSToolkit.Account.View
{
    /// <summary>
    /// Interaction logic for RegisterAccountControl.xaml
    /// </summary>
    public partial class RegisterAccountControl : BaseAWSControl
    {
        // optional callback to indicate to outer container when all mandatory fields
        // have been completed (or not)
        public delegate void MandatoryFieldsReadyCallback(bool fieldsCompleted);
        MandatoryFieldsReadyCallback _fieldsReadyCallback = null;

        RegisterAccountController _controller;

        public RegisterAccountControl()
            : this(null)
        {
        }

        public void SetMandatoryFieldsReadyCallback(MandatoryFieldsReadyCallback callback)
        {
            _fieldsReadyCallback = callback;
        }

        public RegisterAccountControl(RegisterAccountController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
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
            if (string.IsNullOrEmpty(this._controller.Model.DisplayName))
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

        void onRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string url = this._ctlAWSLink.Text;
            Process.Start(new ProcessStartInfo(url));
            e.Handled = true;
        }

        private void MandatoryFieldTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_fieldsReadyCallback != null)
            {
                _fieldsReadyCallback(this._ctlDisplayName.Text.Length > 0
                                        && this._ctlAccessKey.Text.Length > 0
                                        && this._ctlSecretKey.Text.Length > 0);
            }
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
    }
}
