using System.Collections.Specialized;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class EditStreamingDistributionConfigModel : StreamingDistributionConfigModel
    {
        public EditStreamingDistributionConfigModel()
        {
            this.PropertyChanged += OnPropertyChanged;
            this.CNAMEs.CollectionChanged += onCollectionChanged;
            this.TrustedSignerAWSAccountIds.CollectionChanged += onCollectionChanged;
        }

        void onCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.IsDirty = true;
        }


        public void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!string.Equals("IsDirty", e.PropertyName))
            {
                this.IsDirty = true;
            }
        }

        string _id;
        public string Id
        {
            get => this._id;
            set
            {
                this._id = value;
                base.NotifyPropertyChanged("Id");
            }
        }

        string _etag;
        public string ETag
        {
            get => this._etag;
            set
            {
                this._etag = value;
                base.NotifyPropertyChanged("ETag");
            }
        }

        string _domainName;
        public string DomainName
        {
            get => this._domainName;
            set
            {
                this._domainName = value;
                base.NotifyPropertyChanged("DomainName");
            }
        }

        string _status;
        public string Status
        {
            get => this._status;
            set
            {
                this._status = value;
                base.NotifyPropertyChanged("Status");
            }
        }

        string _lastModifedDate;
        public string LastModifedDate
        {
            get => this._lastModifedDate;
            set
            {
                this._lastModifedDate = value;
                base.NotifyPropertyChanged("LastModifedDate");
            }
        }

        bool _isDirty;
        public bool IsDirty
        {
            get => this._isDirty;
            set
            {
                this._isDirty = value;
                base.NotifyPropertyChanged("IsDirty");
            }
        }
    }
}
