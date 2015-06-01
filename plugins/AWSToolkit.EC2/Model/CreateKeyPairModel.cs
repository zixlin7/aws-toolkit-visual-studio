using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateKeyPairModel : BaseModel
    {

        string _keyPairName;
        public string KeyPairName
        {
            get { return this._keyPairName; }
            set
            {
                this._keyPairName = value;
                base.NotifyPropertyChanged("KeyPairName");
            }
        }

        string _privateKey;
        public string PrivateKey
        {
            get { return this._privateKey; }
            set
            {
                this._privateKey = value;
                base.NotifyPropertyChanged("PrivateKey");
            }
        }

        string _fingerprint;
        public string Fingerprint
        {
            get { return this._fingerprint; }
            set
            {
                this._fingerprint = value;
                base.NotifyPropertyChanged("Fingerprint");
            }
        }

        bool _storePrivateKey = true;
        public bool StorePrivateKey
        {
            get { return this._storePrivateKey; }
            set
            {
                this._storePrivateKey = value;
                base.NotifyPropertyChanged("StorePrivateKey");
            }
        }
    }
}
