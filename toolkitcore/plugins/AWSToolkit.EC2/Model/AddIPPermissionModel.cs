using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AddIPPermissionModel : BaseModel
    {

        bool _isPortAndIpChecked = true;
        public bool IsPortAndIpChecked
        {
            get => this._isPortAndIpChecked;
            set
            {
                this._isPortAndIpChecked = value;
                this._isUserAndGroupChecked = !value;
                base.NotifyPropertyChanged("IsPortAndIpChecked");
            }
        }

        bool _isUserAndGroupChecked;
        public bool IsUserAndGroupChecked
        {
            get => this._isUserAndGroupChecked;
            set
            {
                this._isUserAndGroupChecked = value;
                this._isPortAndIpChecked = !value;
                base.NotifyPropertyChanged("IsUserAndGroupChecked");
            }
        }

        NetworkProtocol _ipProtocol = NetworkProtocol.TCP;
        public NetworkProtocol IPProtocol
        {
            get => this._ipProtocol;
            set
            {
                this._ipProtocol = value;
                base.NotifyPropertyChanged("IPProtocol");
            }
        }

        string _portRangeStart;
        public string PortRangeStart
        {
            get => this._portRangeStart;
            set
            {
                this._portRangeStart = value;
                base.NotifyPropertyChanged("PortRangeStart");
            }
        }

        string _portRangeEnd;
        public string PortRangeEnd
        {
            get => this._portRangeEnd;
            set
            {
                this._portRangeEnd = value;
                base.NotifyPropertyChanged("PortRangeEnd");
            }
        }

        string _sourceCIDR = "0.0.0.0/0";
        public string SourceCIDR
        {
            get => this._sourceCIDR;
            set
            {
                this._sourceCIDR = value;
                base.NotifyPropertyChanged("SourceCIDR");
            }
        }

        string _userId;
        public string UserId
        {
            get => this._userId;
            set
            {
                this._userId = value;
                base.NotifyPropertyChanged("UserId");
            }
        }

        string _groupName;
        public string GroupName
        {
            get => this._groupName;
            set
            {
                this._groupName = value;
                base.NotifyPropertyChanged("GroupName");
            }
        }


    }
}
