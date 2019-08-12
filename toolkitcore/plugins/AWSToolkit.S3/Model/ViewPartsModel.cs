using System;
using System.Collections.ObjectModel;
using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class ViewPartsModel : BaseModel
    {
    
        public ViewPartsModel(string bucketName, string key, string uploadId)
        {
            this.BucketName = bucketName;
            this.Key = key;
            this.UploadId = uploadId;
        }

        string _bucketName;
        public string BucketName
        {
            get => this._bucketName;
            set 
            { 
                this._bucketName = value;
                base.NotifyPropertyChanged("BucketName");
            }
        }

        string _key;
        public string Key
        {
            get => this._key;
            set
            {
                this._key = value;
                base.NotifyPropertyChanged("Key");
            }
        }

        string _uploadId;
        public string UploadId
        {
            get => this._uploadId;
            set
            {
                this._uploadId = value;
                base.NotifyPropertyChanged("UploadId");
            }
        }

        ObservableCollection<PartDetailWrapper> _partDetails = new ObservableCollection<PartDetailWrapper>();
        public ObservableCollection<PartDetailWrapper> PartDetails
        {
            get => this._partDetails;
            set
            {
                this._partDetails = value;
                base.NotifyPropertyChanged("PartDetails");
            }
        }


        public class PartDetailWrapper
        {
            PartDetail _partDetail;

            public PartDetailWrapper(PartDetail partDetail)
            {
                this._partDetail = partDetail;
            }

            public long PartNumber => this._partDetail.PartNumber;

            public string FormattedPartNumber => this._partDetail.PartNumber.ToString("#,0");

            public string FormattedSize => this._partDetail.Size.ToString("#,0") + " bytes";

            public DateTime LastModified => this._partDetail.LastModified.ToLocalTime();
        }
    }
}
