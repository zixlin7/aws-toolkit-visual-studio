using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AddNetworkAclRuleModel : BaseModel
    {
        NetworkProtocol _protocol = NetworkProtocol.TCP;
        public NetworkProtocol IPProtocol
        {
            get => this._protocol;
            set
            {
                this._protocol = value;
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

        string _ruleNumber;
        public string RuleNumber
        {
            get => this._ruleNumber;
            set
            {
                this._ruleNumber = value;
                base.NotifyPropertyChanged("RuleNumber");
            }
        }

        bool _isAllow = true;
        public bool IsAllow
        {
            get => this._isAllow;
            set
            {
                this._isAllow = value;
                base.NotifyPropertyChanged("IsAllow");
            }
        }
    }
}
