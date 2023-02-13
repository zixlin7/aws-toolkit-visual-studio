using System;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for OpenSCPSessionControl.xaml
    /// </summary>
    public partial class OpenSCPSessionControl : OpenLinuxToolControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(OpenSSHSessionControl));

        OpenSCPSessionController _controller;

        public OpenSCPSessionControl(OpenSCPSessionController controller, bool useKeyPair, string password)
            : base(controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;

            if (!string.IsNullOrEmpty(this._controller.Model.PrivateKey))
            {
                this._ctlPrivateKeyGrid.Height = 0;
                this.Height -= 160;
            }

            if (useKeyPair)
                this._ctlUseKeypairSelected.IsChecked = true;
            else
                this._ctlUseCustomCredentialsSelected.IsChecked = true;

            this._ctlEnteredPassword.Password = password;
        }

        public override string Title => "Open SCP Session to " + this._controller.InstanceId;


        public override bool OnCommit()
        {
            try
            {
                if (this._ctlUseKeypairSelected.IsChecked.GetValueOrDefault())
                    this._controller.OpenSCPSessionWithEC2KeyPair();
                else
                    this._controller.OpenSCPSessionWithCredentials(this._ctlEnteredPassword.Password);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening scp session: " + e.Message);
                _controller.RecordFailure(e);
                return false;
            }

            return true;
        }
    }
}
