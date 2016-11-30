using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Model
{
    public class ViewMultipartUploadsModel : BaseModel
    {
        public ViewMultipartUploadsModel(string bucketName)
        {
            this._bucketName = bucketName;
        }

        string _bucketName;
        public string BucketName
        {
            get { return this._bucketName; }
            set 
            {
                this._bucketName = value;
                base.NotifyPropertyChanged("BucketName");
            }
        }

        ObservableCollection<MultipartUploadWrapper> _uploads = new ObservableCollection<MultipartUploadWrapper>();
        public ObservableCollection<MultipartUploadWrapper> Uploads
        {
            get { return this._uploads; }
            set
            {
                this._uploads = value;
                base.NotifyPropertyChanged("Uploads");
            }
        }

        public class MultipartUploadWrapper
        {
            MultipartUpload _upload;

            public MultipartUploadWrapper(MultipartUpload upload)
            {
                this._upload = upload;
            }

            public string UploadId
            {
                get { return this._upload.UploadId; }
            }

            public string Key
            {
                get { return this._upload.Key; }
            }

            public string StorageClass
            {
                get 
                {
                    if (this._upload.StorageClass == "STANDARD")
                        return "Standard";

                    return "Reduced Redundancy"; 
                }
            }

            public DateTime Initiated
            {
                get 
                { 
                    return this._upload.Initiated.ToLocalTime(); 
                }
            }



        }
    }
}
