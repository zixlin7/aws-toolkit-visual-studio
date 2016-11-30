using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AttachToVPCModel : BaseModel
    {

        public AttachToVPCModel(string internetGatewayId)
        {
            this.InternetGatewayId = internetGatewayId;
        }

        public string InternetGatewayId
        {
            get;
            set;
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


        public List<VPCWrapper> AvailableVpcs
        {
            get;
            set;
        }
    }
}
