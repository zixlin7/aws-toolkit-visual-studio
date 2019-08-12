using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateVPCModel : BaseModel
    {
        public const string NO_PREFERENCE_ZONE = "No Preference";

        private string _vpcName;

        public string VPCName
        {
            get => this._vpcName;
            set
            {
                this._vpcName = value;
                base.NotifyPropertyChanged("VPCName");
            }
        }

        private string _cidrBlock = "10.0.0.0/16";

        public string CIDRBlock
        {
            get => this._cidrBlock;
            set
            {
                this._cidrBlock = value;
                base.NotifyPropertyChanged("CIDRBlock");
            }
        }

        private bool _withPublicSubnet;

        public bool WithPublicSubnet
        {
            get => this._withPublicSubnet;
            set
            {
                this._withPublicSubnet = value;
                base.NotifyPropertyChanged("WithPublicSubnet");
            }
        }

        private bool _withPrivateSubnet;

        public bool WithPrivateSubnet
        {
            get => this._withPrivateSubnet;
            set
            {
                this._withPrivateSubnet = value;
                base.NotifyPropertyChanged("WithPrivateSubnet");
            }
        }

        private string _instanceTenancy = "default";

        public string InstanceTenancy
        {
            get => this._instanceTenancy;
            set
            {
                this._instanceTenancy = value;
                base.NotifyPropertyChanged("InstanceTenancy");
            }
        }

        private string _publicSubnetCIDRBlock = "10.0.0.0/24";

        public string PublicSubnetCIDRBlock
        {
            get => this._publicSubnetCIDRBlock;
            set
            {
                this._publicSubnetCIDRBlock = value;
                base.NotifyPropertyChanged("PublicSubnetCIDRBlock");
            }
        }

        private string _publicSubnetAvailabilityZone = NO_PREFERENCE_ZONE;

        public string PublicSubnetAvailabilityZone
        {
            get => this._publicSubnetAvailabilityZone;
            set
            {
                this._publicSubnetAvailabilityZone = value;
                base.NotifyPropertyChanged("PublicSubnetAvailabilityZone");
            }
        }

        private string _privateSubnetCIDRBlock = "10.0.1.0/24";

        public string PrivateSubnetCIDRBlock
        {
            get => this._privateSubnetCIDRBlock;
            set
            {
                this._privateSubnetCIDRBlock = value;
                base.NotifyPropertyChanged("PrivateSubnetCIDRBlock");
            }
        }

        private string _privateSubnetAvailabilityZone = NO_PREFERENCE_ZONE;

        public string PrivateSubnetAvailabilityZone
        {
            get => this._privateSubnetAvailabilityZone;
            set
            {
                this._privateSubnetAvailabilityZone = value;
                base.NotifyPropertyChanged("PrivateSubnetAvailabilityZone");
            }
        }

        private InstanceType _natInstanceType = InstanceType.FindById("t2.small");

        public InstanceType NATInstanceType
        {
            get => this._natInstanceType;
            set
            {
                this._natInstanceType = value;
                base.NotifyPropertyChanged("NATInstanceType");
            }
        }

        private string _natKeyPairName;

        public string NATKeyPairName
        {
            get => this._natKeyPairName;
            set
            {
                this._natKeyPairName = value;
                base.NotifyPropertyChanged("NATKeyPairName");
            }
        }

        private bool _configureDefaultVPCGroupForNAT = true;

        public bool ConfigureDefaultVPCGroupForNAT
        {
            get => this._configureDefaultVPCGroupForNAT;
            set
            {
                this._configureDefaultVPCGroupForNAT = value;
                base.NotifyPropertyChanged("ConfigureDefaultVPCGroupForNAT");
            }
        }

        public IList<string> AvailableZones { get; set; }

        public IList<string> KeyPairNames { get; set; }

        public IList<InstanceType> InstanceTypes { get; set; }

        private bool _enableDNSHostnames = true;

        public bool EnableDNSHostnames
        {
            get => _enableDNSHostnames;
            set
            {
                _enableDNSHostnames = value;
                base.NotifyPropertyChanged("EnableDNSHostnames");
            }
        }

        private bool _enableDNSSupport = true;

        public bool EnableDNSSupport
        {
            get => _enableDNSSupport;
            set
            {
                _enableDNSSupport = value;
                base.NotifyPropertyChanged("EnableDNSSupport");
            }
        }
    }
}
