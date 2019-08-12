using System.ComponentModel;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class RouteWrapper : IWrapper
    {
        Route _route;

        public RouteWrapper(Route route)
        {
            this._route = route;
        }

        [Browsable(false)]
        public Route NativeRoute => this._route;

        [Browsable(false)]
        public string DisplayName => this.NativeRoute.DestinationCidrBlock;

        [Browsable(false)]
        public string TypeName => "Route";

        public bool CanDelete => !string.Equals(this._route.GatewayId, "local");

        public string DestinationCidrBlock => this._route.DestinationCidrBlock;

        public string FormattedTarget
        {
            get
            {
                if (!string.IsNullOrEmpty(this._route.GatewayId))
                    return this._route.GatewayId;
                if (!string.IsNullOrEmpty(this._route.InstanceId))
                    return string.Format("{0} / {1}", this._route.NetworkInterfaceId, this._route.InstanceId);
                if (!string.IsNullOrEmpty(this._route.NetworkInterfaceId))
                    return this._route.NetworkInterfaceId;

                return null;
            }
        }

        public string State => this._route.State;
    }
}
