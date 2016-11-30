using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class GetConsoleOutputController
    {
        GetConsoleOutputModel _model;

        public void Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            var response = ec2Client.GetConsoleOutput(new GetConsoleOutputRequest() { InstanceId = instance.InstanceId });
            this._model = new GetConsoleOutputModel();
            this._model.InstanceId = instance.InstanceId;
            this._model.Timestamp = response.Timestamp;

            this._model.ConsoleOutput = StringUtils.DecodeFrom64(response.Output);
            ToolkitFactory.Instance.ShellProvider.ShowModal(new GetConsoleOutputControl(this), System.Windows.MessageBoxButton.OK);
        }

        public GetConsoleOutputModel Model
        {
            get { return this._model; }
        }

    }
}
