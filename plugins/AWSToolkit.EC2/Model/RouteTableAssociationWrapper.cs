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
    public class RouteTableAssociationWrapper : IAssociationWrapper
    {
        RouteTableAssociation _routeTableAssociation;
        Subnet _subnet;

        public RouteTableAssociationWrapper(RouteTableAssociation routeTableAssociation, Subnet subnet)
        {
            this._routeTableAssociation = routeTableAssociation;
            this._subnet = subnet;
        }

        [Browsable(false)]
        public RouteTableAssociation NativeRouteTableAssociation
        {
            get { return this._routeTableAssociation; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this.NativeRouteTableAssociation.RouteTableAssociationId; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Route Table Association"; }
        }

        [Browsable(false)]
        public string AssocationId
        {
            get { return this.RouteTableAssociationId; }
        }

        [DisplayName("Route Table Assocation ID")]
        public string RouteTableAssociationId
        {
            get { return this.NativeRouteTableAssociation.RouteTableAssociationId; }
        }

        [DisplayName("Subnet ID")]
        public string SubnetId
        {
            get { return this.NativeRouteTableAssociation.SubnetId; }
        }

        [DisplayName("CIDR")]
        public string CidrBlock
        {
            get { return this._subnet != null ? this._subnet.CidrBlock : null; }
        }
    }
}
