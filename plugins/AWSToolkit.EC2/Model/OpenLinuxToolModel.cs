using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.EC2.Model
{
    public class OpenLinuxToolModel : BaseModel
    {
        RunningInstanceWrapper _instance;
        public OpenLinuxToolModel(RunningInstanceWrapper instance)
        {
            this._instance = instance;
        }

        string _enteredUsername = "ec2-user";
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

        string _toolLocation;
        public string ToolLocation
        {
            get { return this._toolLocation; }
            set
            {
                this._toolLocation = value;
                base.NotifyPropertyChanged("ToolLocation");
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
