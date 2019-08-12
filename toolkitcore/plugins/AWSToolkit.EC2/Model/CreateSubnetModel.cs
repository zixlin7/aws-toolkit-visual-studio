using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateSubnetModel : BaseModel
    {
        public const string NO_PREFERENCE_ZONE = "No Preference";

        VPCWrapper _vpc;
        public VPCWrapper VPC
        {
            get => this._vpc;
            set
            {
                this._vpc = value;
                base.NotifyPropertyChanged("VPC");
            }
        }

        string _availabilityZone = NO_PREFERENCE_ZONE;
        public string AvailabilityZone
        {
            get => this._availabilityZone;
            set
            {
                this._availabilityZone = value;
                base.NotifyPropertyChanged("AvailabilityZone");
            }
        }

        string _cidrBlock;
        public string CIDRBlock
        {
            get => this._cidrBlock;
            set
            {
                this._cidrBlock = value;
                base.NotifyPropertyChanged("CIDRBlock");
            }
        }

        public List<VPCWrapper> AllVPCs
        {
            get;
            set;
        }

        public List<string> AllAvailabilityZones
        {
            get;
            set;
        }
    }
}
