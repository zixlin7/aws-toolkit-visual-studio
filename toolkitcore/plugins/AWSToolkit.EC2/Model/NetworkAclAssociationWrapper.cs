using System.ComponentModel;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class NetworkAclAssociationWrapper : IWrapper, IAssociationWrapper
    {
        NetworkAclAssociation _routeTableAssociation;
        Subnet _subnet;

        public NetworkAclAssociationWrapper(NetworkAclAssociation networkAclAssociation, Subnet subnet)
        {
            this._routeTableAssociation = networkAclAssociation;
            this._subnet = subnet;
        }

        [Browsable(false)]
        public NetworkAclAssociation NativeNetworkAclAssociation => this._routeTableAssociation;

        [Browsable(false)]
        public string DisplayName => this.NativeNetworkAclAssociation.NetworkAclAssociationId;

        [Browsable(false)]
        public string TypeName => "Network ACL Association";

        [Browsable(false)]
        public string AssocationId => this.NetworkAclAssociationId;

        [DisplayName("Network ACL Assocation ID")]
        public string NetworkAclAssociationId => this.NativeNetworkAclAssociation.NetworkAclAssociationId;

        [DisplayName("Subnet ID")]
        public string SubnetId => this.NativeNetworkAclAssociation.SubnetId;

        [DisplayName("CIDR")]
        public string CidrBlock => this._subnet != null ? this._subnet.CidrBlock : null;
    }
}
