using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.ConnectionUtils;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class OpenSSHSessionController : OpenLinuxToolController
    {
        public override string Executable => SSHUtil.PUTTY_EXECUTABLE;

        public override string ToolSearchFolders => @"";

        protected override Ec2ConnectionType _ec2ConnectionType => Ec2ConnectionType.Ssh;

        public OpenSSHSessionController(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

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
