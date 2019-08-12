﻿using System;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for OpenSSHSessionControl.xaml
    /// </summary>
    public partial class OpenSSHSessionControl : OpenLinuxToolControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(OpenSSHSessionControl));

        OpenSSHSessionController _controller;

        public OpenSSHSessionControl(OpenSSHSessionController controller, bool useKeyPair, string password)
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

        public override string Title => "Open SSH Session to " + this._controller.InstanceId;

        public override bool OnCommit()
        {
            try
            {
                if (this._ctlUseKeypairSelected.IsChecked.GetValueOrDefault())
                    this._controller.OpenSSHSessionWithEC2KeyPair();
                else
                    this._controller.OpenSSHSessionWithCredentials(this._ctlEnteredPassword.Password);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening ssh session: " + e.Message);
                return false;
            }

            return true;
        }
    }
}
