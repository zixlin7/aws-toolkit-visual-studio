using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateKeyPairModel : BaseModel
    {

        string _keyPairName;
        public string KeyPairName
        {
            get => this._keyPairName;
            set
            {
                this._keyPairName = value;
                base.NotifyPropertyChanged("KeyPairName");
            }
        }

        string _privateKey;
        public string PrivateKey
        {
            get => this._privateKey;
            set
            {
                this._privateKey = value;
                base.NotifyPropertyChanged("PrivateKey");
            }
        }

        string _fingerprint;
        public string Fingerprint
        {
            get => this._fingerprint;
            set
            {
                this._fingerprint = value;
                base.NotifyPropertyChanged("Fingerprint");
            }
        }

        bool _storePrivateKey = true;
        public bool StorePrivateKey
        {
            get => this._storePrivateKey;
            set
            {
                this._storePrivateKey = value;
                base.NotifyPropertyChanged("StorePrivateKey");
            }
        }
    }
}
