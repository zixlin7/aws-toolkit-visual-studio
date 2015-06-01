using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Amazon.AWSToolkit.Account.Model;
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

        // used to make the explanatory label of why a profile named 'default'
        // is useful. Not needed if user already has 'default' set up.
        public bool PromptToUseDefaultName
        {
            get { return _controller.PromptToUseDefaultName; }
        }

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
    }
}
