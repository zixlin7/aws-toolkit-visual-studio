using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateNetworkAclModel : BaseModel
    {

        public CreateNetworkAclModel()
        {
        }

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


        public List<VPCWrapper> AvailableVPCs
        {
            get;
            set;
        }
    }
}
