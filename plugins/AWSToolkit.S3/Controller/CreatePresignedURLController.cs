using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit;

using Amazon.S3;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class CreatePresignedURLController : BaseContextCommand
    {
        CreatePresignedURLControl _control;
        CreatePresignedURLModel _model;
        IAmazonS3 _s3Client;

        public override ActionResults Execute(IViewModel model)
        {
            var rootModel = model as S3BucketViewModel;
            if (rootModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(rootModel.S3Client, rootModel.Name, null);
        }

        public ActionResults Execute(IAmazonS3 s3Client, string bucketName, string objectKey)
        {
            this._s3Client = s3Client;
            this._model = new CreatePresignedURLModel(bucketName) { ObjectKey = objectKey };
            this._control = new CreatePresignedURLControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control, MessageBoxButton.OK);

            return new ActionResults().WithSuccess(true);
        }

        public CreatePresignedURLModel Model
        {
            get { return this._model; }
        }

        public void GenerateURL()
        {
            this._model.IsValidURL = false;
            var request = new GetPreSignedUrlRequest()
            {
                BucketName = this._model.BucketName,
                Expires = this._model.Expiration,
                Key = this._model.ObjectKey
            };

            if (this._model.IsGetVerb)
                request.Verb = HttpVerb.GET;
            else if (this._model.IsPutVerb)
            {
                request.Verb = HttpVerb.PUT;
                request.ContentType = this._model.ContentType;
            }

            this._model.FullURL = this._s3Client.GetPreSignedURL(request);
            this._model.IsValidURL = true;
        }
    }
}
