using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.ConnectionUtils;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class OpenSCPSessionController : OpenLinuxToolController
    {
        public override string Executable => SCPUtil.WINSCP_EXECUTABLE;

        public override string ToolSearchFolders => @"C:\Program Files (x86)\WinSCP;C:\Program Files\WinSCP";

        public override OpenLinuxToolControl CreateControl(bool useKeyPair, string password)
        {
            return new OpenSCPSessionControl(this, useKeyPair, password);
        }

        public OpenSCPSessionController(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

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
            SCPUtil.ConnectWithPrivateKey(this._instance.ConnectName, this._model.EnteredUsername, this._model.PrivateKey);
            this._results = new ActionResults().WithSuccess(true);
            PersistLastSelectedValues(true, null);
        }
    }
}
