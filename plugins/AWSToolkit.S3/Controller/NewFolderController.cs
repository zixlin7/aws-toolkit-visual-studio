using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.S3;
using Amazon.S3.IO;

using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.View;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class NewFolderController
    {
        IAmazonS3 _s3Client;
        NewFolderModel _model;

       public NewFolderController(IAmazonS3 s3Client, string bucketName, string parentPath)
            : this(s3Client, new NewFolderModel(bucketName, parentPath))
        {
        }

       public NewFolderController(IAmazonS3 s3Client, NewFolderModel model)
        {
            this._s3Client = s3Client;
            this._model = model;
        }

       public NewFolderModel Model
        {
            get
            {
                return this._model;
            }
        }

        public bool Execute()
        {
            NewFolderControl control = new NewFolderControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void Persist()
        {
            S3Directory.CreateDirectory(
                this._s3Client, 
                this._model.BucketName, 
                this._model.ParentPath + "/" + this._model.NewFolderName);
        }
    }
}
