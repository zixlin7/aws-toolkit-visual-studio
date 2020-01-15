using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.S3;

namespace Amazon.AWSToolkit.S3.Model
{
    public class ObjectPropertiesModel : BaseModel, IMetadataContainerModel, IPermissionContainerModel, ITagContainerModel
    {
        string _bucketName;
        string _key;
        Uri _link;

        S3StorageClass _storageClass;
        bool _useServerSideEncryption;
        ObservableCollection<Metadata> _metadata = new ObservableCollection<Metadata>();
        ObservableCollection<Permission> _permissions = new ObservableCollection<Permission>();
        ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();
        Dictionary<string, string> _originalTags = new Dictionary<string, string>();
        string _websiteRedirectLocation;

        public ObjectPropertiesModel()
        {
        }

        public ObjectPropertiesModel(string bucketName, string key, Uri link)
        {
            this._bucketName = bucketName;
            this._key = key;
            this._link = link;
        }


        public string BucketName => this._bucketName;

        public string Key => this._key;

        public Uri Link => this._link;

        public string Name
        {
            get
            {
                string name;
                int pos = this.Key.LastIndexOf('/');
                if (pos >= 0)
                    name = this.Key.Substring(pos + 1);
                else
                    name = this.Key;

                return name;
            }
        }

        public string WebsiteRedirectLocation
        {
            get => this._websiteRedirectLocation;
            set
            {
                this._websiteRedirectLocation = value;
                base.NotifyPropertyChanged("WebsiteRedirectLocation");
            }
        }

        public string Folder
        {
            get
            {
                int endPos = this.Key.LastIndexOf('/');
                if (endPos <= 0)
                    return string.Empty;

                string folderName;
                int startPos = this.Key.LastIndexOf('/', endPos - 1) + 1;
                if (startPos <= 0)
                    folderName = this.Key.Substring(0, endPos);

                folderName = this.Key.Substring(startPos, endPos - startPos);
                return folderName;
            }
        }

        public string Owner
        {
            get;
            set;
        }

        public string LastModifiedDate
        {
            get;
            set;
        }

        public string ETag
        {
            get;
            set;
        }

        public string Size
        {
            get;
            set;
        }

        public S3StorageClass StorageClass
        {
            get => this._storageClass;
            set
            {
                this._storageClass = value;
                base.NotifyPropertyChanged("StorageClass");
                base.NotifyPropertyChanged("StorageClassValue");
            }
        }

        public string StorageClassValue
        {
            get => this._storageClass.Value;
        }

        public bool StoredInGlacier
        {
            get => Amazon.AWSToolkit.S3.Model.StorageClass.GlacierS3StorageClasses.Contains(this.StorageClass);
        }

        public string RestoreInfo
        {
            get;
            set;
        }

        public bool UseServerSideEncryption
        {
            get => this._useServerSideEncryption;
            set
            {
                this._useServerSideEncryption = value;
                base.NotifyPropertyChanged("UseServerSideEncryption");
            }
        }

        public bool UsesKMSServerSideEncryption
        {
            get;
            set;
        }

        public bool ErrorRetrievingMetadata
        {
            get;
            set;
        }

        public ObservableCollection<Metadata> MetadataEntries
        {
            get => this._metadata;
            set => this._metadata = value;
        }

        public ObservableCollection<Tag> Tags
        {
            get => this._tags;
            set => this._tags = value;
        }

        public Dictionary<string, string> OriginalTags => this._originalTags;

        public bool IsPublic
        {
            get
            {
                foreach (Permission permission in this.PermissionEntries)
                {
                    if (permission.GranteeFormatted != null 
                            && permission.GranteeFormatted.Equals(Permission.CommonURIGrantee.ALL_USERS_URI.Label) 
                            && permission.OpenDownload)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public ObservableCollection<Permission> PermissionEntries
        {
            get => this._permissions;
            set => this._permissions = value;
        }

    }
}
