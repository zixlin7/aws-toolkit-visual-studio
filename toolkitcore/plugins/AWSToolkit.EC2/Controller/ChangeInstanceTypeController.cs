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
    public class ChangeInstanceTypeController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        ChangeInstanceTypeModel _model;
        RunningInstanceWrapper _instance;
        public ActionResults Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            this._model = new ChangeInstanceTypeModel(instance.NativeInstance.InstanceId);
            this._ec2Client = ec2Client;
            this._instance = instance;
            this.LoadModel();
            var control = new ChangeInstanceTypeControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public ChangeInstanceTypeModel Model
        {
            get { return this._model; }
        }

        public void LoadModel()
        {
            var request = new DescribeImagesRequest() { ImageIds = new List<string>() { this._instance.NativeInstance.ImageId } };
            var response = this._ec2Client.DescribeImages(request);

            if(response.Images.Count != 1)
                return;

            IList<InstanceType> validTypes = InstanceType.GetValidTypes(response.Images[0]);

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    this._model.InstanceTypes.Clear();
                    foreach(var type in validTypes)
                    {
                        this._model.InstanceTypes.Add(type);
                    }
                    this._model.SelectedInstanceType = InstanceType.FindById(this._instance.NativeInstance.InstanceType);
                }));

        }

        public void ChangeInstanceType()
        {
            var request = new ModifyInstanceAttributeRequest()
            {
                InstanceId = this.Model.InstanceId,
                Attribute = "instanceType",
                Value = this.Model.SelectedInstanceType.Id
            };
            this._ec2Client.ModifyInstanceAttribute(request);

            this._results = new ActionResults().WithSuccess(true);
            this._instance.InstanceType = this.Model.SelectedInstanceType.Id;
        }
    }
}
