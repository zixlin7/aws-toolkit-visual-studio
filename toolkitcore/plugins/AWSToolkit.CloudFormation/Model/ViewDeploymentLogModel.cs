using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public RunningInstanceWrapper Instance
        {
            get { return this._instance; }
        }

        public string InstanceId
        {
            get { return this._instance.InstanceId; }
        }

        string _log;
        public string Log
        {
            get { return this._log; }
            set { this._log = value; }
        }

        string _errorMessage;
        public string ErrorMessage
        {
            get { return this._errorMessage; }
            set { this._errorMessage = value; }
        }
    }
}
