using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class NewUploadSettingsModel : BaseModel, IMetadataContainerModel, IPermissionContainerModel
    {
        bool _useReduceStorage;
        bool _useServerSideEncryption;
        bool _makePublic;

        ObservableCollection<Metadata> _metadata = new ObservableCollection<Metadata>();
        ObservableCollection<Permission> _permissions = new ObservableCollection<Permission>();

        public bool UseReduceStorage
        {
            get { return this._useReduceStorage; }
            set
            {
                this._useReduceStorage = value;
                base.NotifyPropertyChanged("UseReduceStorage");
            }
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

        public bool MakePublic
        {
            get { return this._makePublic; }
            set
            {
                this._makePublic = value;
                base.NotifyPropertyChanged("MakePublic");
            }
        }

        public ObservableCollection<Metadata> MetadataEntries
        {
            get { return this._metadata; }
            set { this._metadata = value; }
        }


        public ObservableCollection<Permission> PermissionEntries
        {
            get { return this._permissions; }
            set { this._permissions = value; }
        }
    }
}
