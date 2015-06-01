using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
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
        public NetworkAclAssociation NativeNetworkAclAssociation
        {
            get { return this._routeTableAssociation; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this.NativeNetworkAclAssociation.NetworkAclAssociationId; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Network ACL Association"; }
        }

        [Browsable(false)]
        public string AssocationId
        {
            get { return this.NetworkAclAssociationId; }
        }

        [DisplayName("Network ACL Assocation ID")]
        public string NetworkAclAssociationId
        {
            get { return this.NativeNetworkAclAssociation.NetworkAclAssociationId; }
        }

        [DisplayName("Subnet ID")]
        public string SubnetId
        {
            get { return this.NativeNetworkAclAssociation.SubnetId; }
        }

        [DisplayName("CIDR")]
        public string CidrBlock
        {
            get { return this._subnet != null ? this._subnet.CidrBlock : null; }
        }
    }
}
