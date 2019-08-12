using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;

using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.View;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class ViewPartsController
    {
        IAmazonS3 _s3Client;
        ViewPartsModel _model;

        public ViewPartsController(IAmazonS3 s3Client, string bucketName, string key, string uploadId)
        {
            this._s3Client = s3Client;
            this._model = new ViewPartsModel(bucketName, key, uploadId);
        }

        public ViewPartsModel Model => this._model;

        public void Execute()
        {
            var control = new ViewPartsControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control, System.Windows.MessageBoxButton.OK);
        }

        public void LoadModel()
        {
            var lists = new List<ViewPartsModel.PartDetailWrapper>();
            ListPartsRequest request = new ListPartsRequest()
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key,
                UploadId = this._model.UploadId
            };
            ListPartsResponse response = new ListPartsResponse();

            do
            {
                request.PartNumberMarker = response.NextPartNumberMarker.ToString();

                response = this._s3Client.ListParts(request);
                foreach (var partDetail in response.Parts)
                {
                    lists.Add(new ViewPartsModel.PartDetailWrapper(partDetail));
                }

            } while (response.IsTruncated);

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this._model.PartDetails.Clear();
                foreach (var item in lists.OrderBy(item => item.PartNumber))
                    this._model.PartDetails.Add(item);
            }));
        }
    }
}
