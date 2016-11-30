using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            get { return this._decryptedPassword; }
            set
            {
                this._decryptedPassword = value;
                base.NotifyPropertyChanged("DecryptedPassword");
            }
        }

        string _encryptedPassword;
        public string EncryptedPassword
        {
            get { return this._encryptedPassword; }
            set
            {
                this._encryptedPassword = value;
                base.NotifyPropertyChanged("EncryptedPassword");
            }
        }

        public string InstanceId
        {
            get { return this._instance.NativeInstance.InstanceId; }
        }

        public string KeyPair
        {
            get { return this._instance.NativeInstance.KeyName + ".pem"; }
        }

        public string PublicDnsName
        {
            get 
            { 
                return this._instance.NativeInstance.PublicDnsName; 
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



    }
}
