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
    public class CreateImageController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        CreateImageModel _model;
        public ActionResults Execute(IAmazonEC2 ec2Client, RunningInstanceWrapper instance)
        {
            this._model = new CreateImageModel(instance.NativeInstance.InstanceId);
            this._ec2Client = ec2Client;
            var control = new CreateImageControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public CreateImageModel Model
        {
            get { return this._model; }
        }

        public string CreateImage()
        {
            var request = new CreateImageRequest()
            {
                InstanceId = this._model.InstanceId,
                Description = this._model.Description,
                Name = this._model.Name
            };
            var response = this._ec2Client.CreateImage(request);

            var imageId = response.ImageId;
            this._results = new ActionResults().WithFocalname(imageId).WithSuccess(true);
            return imageId;
        }
    }
}
