using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ChangeTerminationProtectionController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        ChangeTerminationProtectionModel _model;
        RunningInstanceWrapper _instance;

        public ActionResults Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            this._model = new ChangeTerminationProtectionModel(instance.NativeInstance.InstanceId);
            this._ec2Client = ec2Client;
            this._instance = instance;
            this.LoadModel();
            var control = new ChangeTerminationProtectionControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public ChangeTerminationProtectionModel Model => this._model;

        public void LoadModel()
        {
            var request = new DescribeInstanceAttributeRequest()
            {
                InstanceId = this._instance.NativeInstance.InstanceId,
                Attribute = "disableApiTermination"
            };
            var response = this._ec2Client.DescribeInstanceAttribute(request);
            
            bool isEnabled = response.InstanceAttribute.DisableApiTermination;

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this._model.IsProtectionEnabled = isEnabled;
                this._model.IsProtectionInitiallyEnabled = isEnabled;
            }));

        }

        public void ChangeTerminationProtection()
        {
            if (this._model.IsProtectionInitiallyEnabled != this._model.IsProtectionEnabled)
            {
                var request = new ModifyInstanceAttributeRequest()
                {
                    InstanceId = this.Model.InstanceId,
                    Attribute = "disableApiTermination",
                    Value = (this.Model.IsProtectionEnabled).ToString()
                };
                this._ec2Client.ModifyInstanceAttribute(request);

                this._results = new ActionResults().WithSuccess(true);
            }
        }
    }
}
