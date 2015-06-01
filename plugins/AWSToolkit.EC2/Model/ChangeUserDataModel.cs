using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ChangeUserDataModel : BaseModel
    {
        RunningInstanceWrapper _instance;
        public ChangeUserDataModel(RunningInstanceWrapper instance)
        {
            this._instance = instance;
        }

        public string InstanceId
        {
            get { return this._instance.NativeInstance.InstanceId; }
        }

        public bool IsReadOnly
        {
            get { return this._instance.NativeInstance.State.Name != EC2Constants.INSTANCE_STATE_STOPPED; }
        }

        string _userData;
        public string UserData
        {
            get 
            {
                return this._userData; 
            }
            set
            {
                this._userData = value;
                base.NotifyPropertyChanged("UserData");
            }
        }

        public string ConvertedUserData
        {
            get
            {
                if (this._isBase64Encoded)
                    return StringUtils.EncodeTo64(this.UserData);

                return this.UserData;
            }
            set
            {
                if (this._isBase64Encoded)
                    this.UserData = StringUtils.DecodeFrom64(value);
                else
                    this.UserData = value;
                base.NotifyPropertyChanged("ConvertedUserData");
            }
        }

        string _initialUserData;
        public string InitialUserData
        {
            get { return this._initialUserData; }
            set
            {
                this._initialUserData = value;
                base.NotifyPropertyChanged("InitialUserData");
            }
        }

        bool _isBase64Encoded;
        public bool IsBase64Encoded
        {
            get { return this._isBase64Encoded; }
            set
            {
                this._isBase64Encoded = value;
                base.NotifyPropertyChanged("IsBase64Encoded");
                base.NotifyPropertyChanged("ConvertedUserData");
            }
        }
     }
}
