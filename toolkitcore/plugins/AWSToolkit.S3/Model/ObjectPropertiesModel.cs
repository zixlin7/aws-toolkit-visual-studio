using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class ObjectPropertiesModel : BaseModel, IMetadataContainerModel, IPermissionContainerModel, ITagContainerModel
    {
        string _bucketName;
        string _key;
        Uri _link;

        bool _useReducedRedundancyStorage;
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


        public string BucketName
        {
            get { return this._bucketName; }
        }

        public string Key
        {
            get { return this._key; }
        }

        public Uri Link
        {
            get { return this._link; }
        }

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
            get { return this._websiteRedirectLocation; }
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

        public bool UseReducedRedundancyStorage
        {
            get { return this._useReducedRedundancyStorage; }
            set
            {
                this._useReducedRedundancyStorage = value;
                base.NotifyPropertyChanged("UseReducedRedundancyStorage");
            }
        }

        public bool StoredInGlacier
        {
            get;
            set;
        }

        public string RestoreInfo
        {
            get;
            set;
        }

        public bool UseServerSideEncryption
        {
            get { return this._useServerSideEncryption; }
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
            get { return this._metadata; }
            set { this._metadata = value; }
        }

        public ObservableCollection<Tag> Tags
        {
            get { return this._tags; }
            set { this._tags = value; }
        }

        public Dictionary<string, string> OriginalTags
        {
            get { return this._originalTags; }
        }

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
            get { return this._permissions; }
            set { this._permissions = value; }
        }

    }
}
