using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class ViewDeploymentLogModel : BaseModel
    {
        RunningInstanceWrapper _instance;

        public ViewDeploymentLogModel(RunningInstanceWrapper instance)
        {
            this._instance = instance;
        }

        public RunningInstanceWrapper Instance => this._instance;

        public string InstanceId => this._instance.InstanceId;

        string _log;
        public string Log
        {
            get => this._log;
            set => this._log = value;
        }

        string _errorMessage;
        public string ErrorMessage
        {
            get => this._errorMessage;
            set => this._errorMessage = value;
        }
    }
}
