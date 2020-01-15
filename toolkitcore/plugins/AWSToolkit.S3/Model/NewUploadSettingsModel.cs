using System.Collections.ObjectModel;
using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class NewUploadSettingsModel : BaseModel, IMetadataContainerModel, IPermissionContainerModel, ITagContainerModel
    {
        bool _useServerSideEncryption;
        bool _makePublic;

        ObservableCollection<Metadata> _metadata = new ObservableCollection<Metadata>();
        ObservableCollection<Permission> _permissions = new ObservableCollection<Permission>();
        ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();

        public bool UseServerSideEncryption
        {
            get => this._useServerSideEncryption;
            set
            {
                this._useServerSideEncryption = value;
                base.NotifyPropertyChanged("UseServerSideEncryption");
            }
        }

        public bool MakePublic
        {
            get => this._makePublic;
            set
            {
                this._makePublic = value;
                base.NotifyPropertyChanged("MakePublic");
            }
        }

        public ObservableCollection<Metadata> MetadataEntries
        {
            get => this._metadata;
            set => this._metadata = value;
        }


        public ObservableCollection<Permission> PermissionEntries
        {
            get => this._permissions;
            set => this._permissions = value;
        }

        public ObservableCollection<Tag> Tags
        {
            get => this._tags;
            set => this._tags = value;
        }
    }
}
