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
    public class OpenSCPSessionController : OpenLinuxToolController
    {
        public override string Executable
        {
            get { return SCPUtil.WINSCP_EXECUTABLE; }
        }

        public override string ToolSearchFolders
        {
            get { return @"C:\Program Files (x86)\WinSCP;C:\Program Files\WinSCP"; }
        }

        public override OpenLinuxToolControl CreateControl(bool useKeyPair, string password)
        {
            return new OpenSCPSessionControl(this, useKeyPair, password);
        }

        public void OpenSCPSessionWithCredentials(string password)
        {
            CheckIfOpenPort();

            ToolsUtil.SetToolLocation(Executable, this._model.ToolLocation);
            SCPUtil.Connect(this._instance.ConnectName, this._model.EnteredUsername, password);
            this._results = new ActionResults().WithSuccess(true);
            PersistLastSelectedValues(false, password);
        }

        public void OpenSCPSessionWithEC2KeyPair()
        {
            CheckIfOpenPort();

            ToolsUtil.SetToolLocation(Executable, this._model.ToolLocation);
            SCPUtil.ConnectWithPrivateKey(this._instance.NativeInstance.PublicIpAddress, this._model.EnteredUsername, this._model.PrivateKey);
            this._results = new ActionResults().WithSuccess(true);
            PersistLastSelectedValues(true, null);
        }
    }
}
