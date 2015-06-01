using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

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
        public Route NativeRoute
        {
            get { return this._route; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this.NativeRoute.DestinationCidrBlock; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Route"; }
        }

        public bool CanDelete
        {
            get { return !string.Equals(this._route.GatewayId, "local"); }
        }

        public string DestinationCidrBlock
        {
            get { return this._route.DestinationCidrBlock; }
        }

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

        public string State
        {
            get { return this._route.State; }
        }

    }
}
