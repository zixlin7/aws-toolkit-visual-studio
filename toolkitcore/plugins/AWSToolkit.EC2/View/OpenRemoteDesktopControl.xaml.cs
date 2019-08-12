using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for OpenDesktopControl.xaml
    /// </summary>
    public partial class OpenRemoteDesktopControl : BaseAWSControl
    {
       static ILog LOGGER = LogManager.GetLogger(typeof(CreateVolumeControl));

        OpenRemoteDesktopController _controller;

        public OpenRemoteDesktopControl(OpenRemoteDesktopController controller, bool useKeyPair, string password)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;

            if (!string.IsNullOrEmpty(this._controller.Model.PrivateKey))
            {
                this._ctlPrivateKeyGrid.Height = 0;
                this.Height -= 160;
            }

            if(useKeyPair)
                this._ctlUseKeypairSelected.IsChecked = true;
            else
                this._ctlUseCustomCredentialsSelected.IsChecked = true;
            
            this._ctlEnteredPassword.Password = password;
        }

        public override string Title => "Open Remote Desktop to " + this._controller.InstanceId;

        public override bool OnCommit()
        {
            try
            {
                if (this._ctlUseKeypairSelected.IsChecked.GetValueOrDefault())
                    this._controller.OpenRemoteDesktopWithEC2KeyPair();
                else
                    this._controller.OpenRemoteDesktopWithCredentials(this._controller.Model.EnteredUsername, this._ctlEnteredPassword.Password);
            }
            catch(Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening remote desktop: " + e.Message);
                return false;
            }

            return true;
        }
    }
}
