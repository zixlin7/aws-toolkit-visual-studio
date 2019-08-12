using System.ComponentModel;
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
        public RouteTableAssociation NativeRouteTableAssociation => this._routeTableAssociation;

        [Browsable(false)]
        public string DisplayName => this.NativeRouteTableAssociation.RouteTableAssociationId;

        [Browsable(false)]
        public string TypeName => "Route Table Association";

        [Browsable(false)]
        public string AssocationId => this.RouteTableAssociationId;

        [DisplayName("Route Table Assocation ID")]
        public string RouteTableAssociationId => this.NativeRouteTableAssociation.RouteTableAssociationId;

        [DisplayName("Subnet ID")]
        public string SubnetId => this.NativeRouteTableAssociation.SubnetId;

        [DisplayName("CIDR")]
        public string CidrBlock => this._subnet != null ? this._subnet.CidrBlock : null;
    }
}
