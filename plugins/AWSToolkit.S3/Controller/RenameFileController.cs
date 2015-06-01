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
    public class RenameFileController
    {
        IAmazonS3 _s3Client;
        RenameFileModel _model;

       public RenameFileController(IAmazonS3 s3Client, string bucketName, string key)
            : this(s3Client, new RenameFileModel(bucketName, key))
        {
        }

        public RenameFileController(IAmazonS3 s3Client, RenameFileModel model)
        {
            this._s3Client = s3Client;
            this._model = model;
        }

        public RenameFileModel Model
        {
            get
            {
                return this._model;
            }
        }

        public bool Execute()
        {
            RenameFileControl control = new RenameFileControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void Persist()
        {
            S3File.Move(this._s3Client, this._model.BucketName, this._model.OrignalFullPathKey,
                    this._model.BucketName, this._model.NewFullPathKey);
        }
    }
}
