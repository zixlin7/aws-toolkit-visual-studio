using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ConnectionUtils;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class OpenSSHSessionController : OpenLinuxToolController
    {
        public override string Executable
        {
            get { return SSHUtil.PUTTY_EXECUTABLE; }
        }

        public override string ToolSearchFolders
        {
            get { return @""; }
        }

        public override OpenLinuxToolControl CreateControl(bool useKeyPair, string password)
        {
            return new OpenSSHSessionControl(this, useKeyPair, password);
        }

        public void OpenSSHSessionWithCredentials(string password)
        {
            CheckIfOpenPort();

            ToolsUtil.SetToolLocation(Executable, this._model.ToolLocation);
            SSHUtil.Connect(this._instance.ConnectName, this._model.EnteredUsername, password);
            this._results = new ActionResults().WithSuccess(true);
            PersistLastSelectedValues(false, password);
        }

        public void OpenSSHSessionWithEC2KeyPair()
        {
            CheckIfOpenPort();

            ToolsUtil.SetToolLocation(Executable, this._model.ToolLocation);
            SSHUtil.ConnectWithPrivateKey(this._instance.ConnectName, this._model.EnteredUsername, this._model.PrivateKey);
            this._results = new ActionResults().WithSuccess(true);
            PersistLastSelectedValues(true, null);
        }
    }
}
