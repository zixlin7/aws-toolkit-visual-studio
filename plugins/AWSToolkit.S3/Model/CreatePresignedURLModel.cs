using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class CreatePresignedURLModel : BaseModel
    {
        public CreatePresignedURLModel(string bucketName)
        {
            this.BucketName = bucketName;
            Expiration = DateTime.Now.AddHours(1);
            IsGetVerb = true;
        }

        string _bucketName;
        public string BucketName
        {
            get { return this._bucketName; }
            set
            {
                this._bucketName = value;
                this.NotifyPropertyChanged("BucketName");
            }
        }

        string _objectKey;
        public string ObjectKey
        {
            get { return this._objectKey; }
            set
            {
                this._objectKey = value;
                this.NotifyPropertyChanged("ObjectKey");
            }
        }

        string _contentType;
        public string ContentType
        {
            get { return this._contentType; }
            set
            {
                this._contentType = value;
                this.NotifyPropertyChanged("ContentType");
            }
        }

        DateTime _expiration;
        public DateTime Expiration
        {
            get { return this._expiration; }
            set
            {
                this._expiration = value;
                this.NotifyPropertyChanged("Expiration");
            }
        }

        bool _isGetVerb = true;
        public bool IsGetVerb
        {
            get { return this._isGetVerb; }
            set
            {
                if (this._isGetVerb != value)
                {
                    this._isGetVerb = value;
                    this._isPutVerb = !this._isGetVerb;
                    this.NotifyPropertyChanged("IsGetVerb");
                    this.NotifyPropertyChanged("IsPutVerb");
                }
            }
        }


        bool _isPutVerb;
        public bool IsPutVerb
        {
            get { return this._isPutVerb; }
            set
            {
                if (this._isPutVerb != value)
                {
                    this._isPutVerb = value;
                    this._isGetVerb = !this._isPutVerb;
                    this.NotifyPropertyChanged("IsPutVerb");
                    this.NotifyPropertyChanged("IsGetVerb");
                }
            }
        }

        string _fullURL;
        public string FullURL
        {
            get { return this._fullURL; }
            set
            {
                this._fullURL = value;
                this.NotifyPropertyChanged("FullURL");
            }
        }

        bool _isValidURL;
        public bool IsValidURL
        {
            get { return this._isValidURL; }
            set
            {
                this._isValidURL = value;
                this.NotifyPropertyChanged("IsValidURL");
            }
        }

    }
}
