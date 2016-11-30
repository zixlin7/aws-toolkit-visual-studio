using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class OpenRemoteDesktopModel : BaseModel
    {
        RunningInstanceWrapper _instance;
        public OpenRemoteDesktopModel(RunningInstanceWrapper instance)
        {
            this._instance = instance;
        }

        string _enteredUsername;
        public string EnteredUsername
        {
            get { return this._enteredUsername; }
            set
            {
                this._enteredUsername = value;
                base.NotifyPropertyChanged("EnteredUsername");
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

        bool _mapDrives = true;
        public bool MapDrives
        {
            get { return this._mapDrives; }
            set
            {
                this._mapDrives = value;
                base.NotifyPropertyChanged("MapDrives");
            }
        }

        bool _saveCredentials = true;
        public bool SaveCredentials
        {
            get { return this._saveCredentials; }
            set
            {
                this._saveCredentials = value;
                base.NotifyPropertyChanged("SaveCredentials");
            }
        }

        bool _savePrivateKey = true;
        public bool SavePrivateKey
        {
            get { return this._savePrivateKey; }
            set
            {
                this._savePrivateKey = value;
                base.NotifyPropertyChanged("SavePrivateKey");
            }
        }
    }
}
