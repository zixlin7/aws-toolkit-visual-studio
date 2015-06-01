using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

using log4net;


namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for GetPasswordControl.xaml
    /// </summary>
    public partial class GetPasswordControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateVolumeControl));

        GetPasswordController _controller;

        public GetPasswordControl(GetPasswordController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;

            if (string.IsNullOrEmpty(this._controller.Model.DecryptedPassword))
            {
                this._ctlAskPrivateKey.Visibility = Visibility.Visible;
                this._ctlAskPrivateKey.Opacity = 1;
                this._ctlShowPassword.Opacity = 0;
            }
            else
            {
                this._ctlShowPassword.Visibility = Visibility.Visible;
                this._ctlAskPrivateKey.Opacity = 0;
                this._ctlShowPassword.Opacity = 1;
            }
        }

        public override string Title
        {
            get
            {
                return "Windows Administrator Password";
            }
        }

        public override bool Validated()
        {
            if (this._ctlAskPrivateKey.Visibility != Visibility.Visible)
                return true;

            if (string.IsNullOrEmpty(this._controller.Model.PrivateKey))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Private key is a required field");
                return false;
            }

            if (!this._controller.Model.PrivateKey.Trim().StartsWith("-----BEGIN RSA PRIVATE KEY-----") ||
                !this._controller.Model.PrivateKey.Trim().EndsWith("-----END RSA PRIVATE KEY-----"))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Invalid RSA private key format");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                if (this._ctlShowPassword.Visibility != Visibility.Visible)
                {
                    this._controller.DecryptPassword();
                    BeginStoryboard storyboard =
                        FindResource("FadeInToShow") as BeginStoryboard;
                    BeginStoryboard(storyboard.Storyboard);
                    this._ctlShowPassword.Visibility = Visibility.Visible;

                    return false;
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error decrypting password: " + e.Message);
                return false;
            }
            return true;
        }
    }
}
