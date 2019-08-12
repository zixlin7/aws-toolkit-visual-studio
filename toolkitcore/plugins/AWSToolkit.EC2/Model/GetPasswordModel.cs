using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class GetPasswordModel : BaseModel
    {
        RunningInstanceWrapper _instance;

        public GetPasswordModel(RunningInstanceWrapper instance)
        {
            this._instance = instance;
        }

        string _decryptedPassword;
        public string DecryptedPassword
        {
            get => this._decryptedPassword;
            set
            {
                this._decryptedPassword = value;
                base.NotifyPropertyChanged("DecryptedPassword");
            }
        }

        string _encryptedPassword;
        public string EncryptedPassword
        {
            get => this._encryptedPassword;
            set
            {
                this._encryptedPassword = value;
                base.NotifyPropertyChanged("EncryptedPassword");
            }
        }

        public string InstanceId => this._instance.NativeInstance.InstanceId;

        public string KeyPair => this._instance.NativeInstance.KeyName + ".pem";

        public string PublicDnsName => this._instance.NativeInstance.PublicDnsName;

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



    }
}
