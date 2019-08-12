using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.EC2.Model
{
    public class ChangeTerminationProtectionModel : BaseModel
    {
        public ChangeTerminationProtectionModel(string instanceId)
        {
            this._instanceId = instanceId;
        }

        string _instanceId;
        public string InstanceId
        {
            get => this._instanceId;
            set
            {
                this._instanceId = value;
                base.NotifyPropertyChanged("InstanceId");
            }
        }

        bool _isProtectionEnabled;
        public bool IsProtectionEnabled
        {
            get => this._isProtectionEnabled;
            set
            {
                this._isProtectionEnabled = value;
                base.NotifyPropertyChanged("IsProtectionEnabled");
            }
        }

        bool _isProtectionInitiallyEnabled;
        public bool IsProtectionInitiallyEnabled
        {
            get => this._isProtectionInitiallyEnabled;
            set
            {
                this._isProtectionInitiallyEnabled = value;
                base.NotifyPropertyChanged("IsProtectionInitiallyEnabled");
            }
        }
    }
}
