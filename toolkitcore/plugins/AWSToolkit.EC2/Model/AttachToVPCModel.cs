using System.Collections.Generic;
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
            get => this._vpc;
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
