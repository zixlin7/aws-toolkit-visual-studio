using System;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.CloudFront.View;

namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class CreateOriginAccessIdentityController
    {
        IAmazonCloudFront _cfClient;
        CreateOriginAccessIdentityModel _model;

        public CreateOriginAccessIdentityController(IAmazonCloudFront cfClient)
        {
            this._cfClient = cfClient;
            this._model = new CreateOriginAccessIdentityModel();
        }

        public CreateOriginAccessIdentityModel Model => this._model;

        public bool Execute()
        {
            CreateOriginAccessIdentityControl control = new CreateOriginAccessIdentityControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void Persist()
        {
            var request = new CreateCloudFrontOriginAccessIdentityRequest();
            request.CloudFrontOriginAccessIdentityConfig = new CloudFrontOriginAccessIdentityConfig();
            request.CloudFrontOriginAccessIdentityConfig.Comment = this._model.Comment;
            request.CloudFrontOriginAccessIdentityConfig.CallerReference = Guid.NewGuid().ToString();
            var response = this._cfClient.CreateCloudFrontOriginAccessIdentity(request);
            this._model.OriginAccessIdentity = response.CloudFrontOriginAccessIdentity;
        }

    }
}
