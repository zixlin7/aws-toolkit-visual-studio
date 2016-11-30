using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class CreateBucketModel : BaseModel
    {
        string _bucketName;

        public CreateBucketModel()
        {
        }

        public string BucketName
        {
            get { return this._bucketName; }
            set
            {
                this._bucketName = value;
                this.NotifyPropertyChanged("BucketName");
            }
        }
    }
}
