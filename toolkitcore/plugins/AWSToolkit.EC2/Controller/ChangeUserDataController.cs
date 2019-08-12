using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ChangeUserDataController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        ChangeUserDataModel _model;
        RunningInstanceWrapper _instance;

        public ActionResults Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            this._model = new ChangeUserDataModel(instance);
            this._ec2Client = ec2Client;
            this._instance = instance;
            this.LoadModel();
            var control = new ChangeUserDataControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public ChangeUserDataModel Model => this._model;

        public void LoadModel()
        {
            var request = new DescribeInstanceAttributeRequest()
            {
                InstanceId = this._instance.NativeInstance.InstanceId,
                Attribute = "userData"
            };
            var response = this._ec2Client.DescribeInstanceAttribute(request);

            string userData = response.InstanceAttribute.UserData;
            userData = StringUtils.DecodeFrom64(userData);
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this._model.UserData = userData;
                this._model.InitialUserData = userData;
            }));

        }

        public void ChangeUserData()
        {
            if (this._model.InitialUserData != this._model.UserData)
            {
                var request = new ModifyInstanceAttributeRequest()
                {
                    InstanceId = this.Model.InstanceId,
                    Attribute = "userData",
                    Value = this.Model.UserData
                };
                this._ec2Client.ModifyInstanceAttribute(request);

                this._results = new ActionResults().WithSuccess(true);
            }
        }
    }
}
