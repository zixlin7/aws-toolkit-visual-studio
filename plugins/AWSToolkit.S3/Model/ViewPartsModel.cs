﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
            get { return this._bucketName; }
            set 
            { 
                this._bucketName = value;
                base.NotifyPropertyChanged("BucketName");
            }
        }

        string _key;
        public string Key
        {
            get { return this._key; }
            set
            {
                this._key = value;
                base.NotifyPropertyChanged("Key");
            }
        }

        string _uploadId;
        public string UploadId
        {
            get { return this._uploadId; }
            set
            {
                this._uploadId = value;
                base.NotifyPropertyChanged("UploadId");
            }
        }

        ObservableCollection<PartDetailWrapper> _partDetails = new ObservableCollection<PartDetailWrapper>();
        public ObservableCollection<PartDetailWrapper> PartDetails
        {
            get { return this._partDetails; }
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

            public long PartNumber
            {
                get { return this._partDetail.PartNumber; }
            }

            public string FormattedPartNumber
            {
                get
                {
                    return this._partDetail.PartNumber.ToString("#,0");
                }
            }

            public string FormattedSize
            {
                get
                {
                    return this._partDetail.Size.ToString("#,0") + " bytes";
                }
            }

            public DateTime LastModified
            {
                get
                {
                    return this._partDetail.LastModified.ToLocalTime();
                }
            }
        }
    }
}
