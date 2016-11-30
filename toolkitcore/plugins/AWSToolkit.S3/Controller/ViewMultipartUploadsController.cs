using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit;

using Amazon.S3;
using Amazon.S3.Model;


using log4net;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class ViewMultipartUploadsController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewMultipartUploadsController));

        IAmazonS3 _s3Client;

        ViewMultipartUploadsControl _control;
        ViewMultipartUploadsModel _model;

        public ViewMultipartUploadsModel Model
        {
            get { return this._model; }
        }

        public override ActionResults Execute(IViewModel model)
        {
            S3BucketViewModel bucketModel = model as S3BucketViewModel;
            if (bucketModel == null)
                return new ActionResults().WithSuccess(false);


            this._s3Client = bucketModel.S3Client;
            this._model = new ViewMultipartUploadsModel(bucketModel.Name);
            this._control = new ViewMultipartUploadsControl(this);

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public void Refresh()
        {
            this.LoadModel();
        }

        public void LoadModel()
        {
            var lists = new List<ViewMultipartUploadsModel.MultipartUploadWrapper>();
            ListMultipartUploadsRequest request = new ListMultipartUploadsRequest() { BucketName = this._model.BucketName };
            ListMultipartUploadsResponse response = new ListMultipartUploadsResponse();

            do
            {
                request.UploadIdMarker = response.NextUploadIdMarker;
                request.KeyMarker = response.KeyMarker;

                response = this._s3Client.ListMultipartUploads(request);
                foreach (var multipart in response.MultipartUploads)
                {
                    lists.Add(new ViewMultipartUploadsModel.MultipartUploadWrapper(multipart));
                }

            } while (response.IsTruncated);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() => 
            {
                this._model.Uploads.Clear();
                foreach (var item in lists.OrderBy(item => item.Key))
                    this._model.Uploads.Add(item);
            }));
        }

        public void AbortUploads(IList<ViewMultipartUploadsModel.MultipartUploadWrapper> items)
        {
            foreach (var item in items)
            {
                this._s3Client.AbortMultipartUpload(new AbortMultipartUploadRequest()
                {
                    BucketName = this._model.BucketName,
                    Key = item.Key,
                    UploadId = item.UploadId
                });

                this._model.Uploads.Remove(item);
            }
        }

        public void DisplayParts(ViewMultipartUploadsModel.MultipartUploadWrapper upload)
        {
            ViewPartsController controller = new ViewPartsController(this._s3Client, this._model.BucketName, upload.Key, upload.UploadId);
            controller.Execute();
        }
    }
}
