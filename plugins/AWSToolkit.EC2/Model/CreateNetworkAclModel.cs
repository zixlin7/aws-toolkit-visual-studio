using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            get
            {
                return this._vpc;
            }
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
