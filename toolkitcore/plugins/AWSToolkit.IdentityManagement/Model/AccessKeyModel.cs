using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class AccessKeyModel : BaseModel
    {
        public const string STATUS_ACTIVE = "Active";
        public const string STATUS_INACTIVE = "Inactive";

        public AccessKeyModel()
        {
        }

        public AccessKeyModel(string accessKey, string status, DateTime createDate)
        {
            this._accessKey = accessKey;
            this._status = status;
            this._createDate = createDate;
        }

        public AccessKeyModel(string accessKey, string status, DateTime createDate, bool persistSecretKeyLocal, string secretKey)
            : this(accessKey, status, createDate)
        {
            this._persistSecretKeyLocal = persistSecretKeyLocal;
            this._secretKey = secretKey;
        }

        string _accessKey;
        public string AccessKey
        {
            get { return this._accessKey; }
            set
            {
                this._accessKey = value;
                base.NotifyPropertyChanged("AccessKey");
            }
        }

        string _status;
        public string Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                base.NotifyPropertyChanged("Status");
                base.NotifyPropertyChanged("ChangeStatusLabel");
            }
        }

        public string ChangeStatusLabel
        {
            get 
            {
                if (this.Status == STATUS_INACTIVE)
                    return string.Format("Make {0}", STATUS_ACTIVE);
                return string.Format("Make {0}", STATUS_INACTIVE); 
            }
        }

        DateTime _createDate;
        public DateTime CreateDate
        {
            get { return this._createDate; }
            set
            {
                this._createDate = value;
                base.NotifyPropertyChanged("CreateDate");
            }
        }


        bool _persistSecretKeyLocal;
        public bool PersistSecretKeyLocal
        {
            get { return this._persistSecretKeyLocal; }
            set
            {
                if (this._persistSecretKeyLocal != value)
                {
                    this._persistSecretKeyLocal = value;
                    persistSecretKey();
                    base.NotifyPropertyChanged("PersistSecretKeyLocal");
                }
            }
        }

        string _secretKey;
        public string SecretKey
        {
            get { return this._secretKey; }
            set
            {
                this._secretKey = value;
                persistSecretKey();
                base.NotifyPropertyChanged("SecretKey");
            }
        }

        void persistSecretKey()
        {
            SettingsCollection collection = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.SecretKeyRepository);
            var keys = collection[ToolkitSettingsConstants.SecretKeyRepository];

            if (this.PersistSecretKeyLocal)
                keys[this.AccessKey] = this.SecretKey;
            else
                keys.Remove(this.AccessKey);

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.SecretKeyRepository, collection);
        }

        public static SettingsCollection.ObjectSettings LoadSecretKeysLocalRepository()
        {
            SettingsCollection collection = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.SecretKeyRepository);
            return collection[ToolkitSettingsConstants.SecretKeyRepository];
        }
    }
}
