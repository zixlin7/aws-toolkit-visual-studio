using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ChangeShutdownBehaviorController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        ChangeShutdownBehaviorModel _model;
        RunningInstanceWrapper _instance;
        public ActionResults Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            this._model = new ChangeShutdownBehaviorModel(instance.NativeInstance.InstanceId);
            this._ec2Client = ec2Client;
            this._instance = instance;
            this.LoadModel();
            var control = new ChangeShutdownBehaviorControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public ChangeShutdownBehaviorModel Model
        {
            get { return this._model; }
        }

        public void LoadModel()
        {
            var request = new DescribeInstanceAttributeRequest()
            {
                InstanceId = this._instance.NativeInstance.InstanceId,
                Attribute = "instanceInitiatedShutdownBehavior"
            };
            var response = this._ec2Client.DescribeInstanceAttribute(request);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                this._model.SelectedOption = response.InstanceAttribute.InstanceInitiatedShutdownBehavior;
            }));

        }

        public void ChangeShutdownBehavior()
        {
            if (!string.Equals(this._model.SelectedOption, this._model.InitialOption))
            {
                var request = new ModifyInstanceAttributeRequest()
                {
                    InstanceId = this._instance.NativeInstance.InstanceId,
                    Attribute = "instanceInitiatedShutdownBehavior",
                    Value = this.Model.SelectedOption
                };
                this._ec2Client.ModifyInstanceAttribute(request);

                this._results = new ActionResults().WithSuccess(true);
            }
        }
    }
}
