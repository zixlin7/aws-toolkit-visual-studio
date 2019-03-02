using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewRouteTablesController : FeatureController<ViewRouteTablesModel>, ISubnetAssociationController
    {
        ViewRouteTablesControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewRouteTablesControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshRouteTables();
        }

        public RouteTableWrapper SetMainRouteTable(RouteTableWrapper routeTable)
        {
            var controller = new SetMainRouteTableController();
            var results = controller.Execute(this.EC2Client, routeTable);
            if (results.Success)
            {
                this.RefreshRouteTables();
                foreach (var table in this.Model.RouteTables)
                {
                    if (string.Equals(table.RouteTableId, routeTable.RouteTableId))
                        return table;
                }
            }

            return null;
        }

        public RouteWrapper AddRoute(RouteTableWrapper routeTable)
        {
            var controller = new AddRouteController();
            var results = controller.Execute(this.EC2Client, routeTable);
            if (results.Success)
            {
                this.RefreshRoutes(routeTable);
                foreach (var route in routeTable.Routes)
                {
                    if (string.Equals(route.DestinationCidrBlock, results.FocalName))
                        return route;
                }
            }

            return null;
        }

        public void DeleteRoutes(string routeTableId, IEnumerable<RouteWrapper> routes)
        {
            foreach (var route in routes)
            {
                var deleteRequest = new DeleteRouteRequest() { RouteTableId = routeTableId, DestinationCidrBlock = route.DestinationCidrBlock };
                this.EC2Client.DeleteRoute(deleteRequest);
            }
        }

        public void RefreshRoutes(RouteTableWrapper routeTable)
        {
            var request = new DescribeRouteTablesRequest() { RouteTableIds = new List<string>(){routeTable.RouteTableId} };
            var response = this.EC2Client.DescribeRouteTables(request);
            var refreshedTable = response.RouteTables.FirstOrDefault(x => x.RouteTableId == routeTable.RouteTableId);
            
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                routeTable.Routes.Clear();
                if (refreshedTable != null)
                {
                    foreach (var item in refreshedTable.Routes)
                    {
                        routeTable.Routes.Add(new RouteWrapper(item));
                    }
                }
            }));
        }

        public object AddSubnetAssociation(IWrapper wrapper)
        {
            var routeTable = wrapper as RouteTableWrapper;

            var controller = new AssociateSubnetToRouteTableController();
            var results = controller.Execute(this.EC2Client, routeTable);
            if (results.Success)
            {
                this.RefreshAssociations(routeTable);
                foreach (var association in routeTable.Associations)
                {
                    if (string.Equals(association.SubnetId, results.FocalName))
                        return association;
                }
            }

            return null;
        }

        public void DisassociateSubnets(string vpcId, IEnumerable<string> associationIds)
        {
            foreach (var associationid in associationIds)
            {
                var request = new DisassociateRouteTableRequest() { AssociationId = associationid };
                this.EC2Client.DisassociateRouteTable(request);
            }
        }

        public void RefreshAssociations(IWrapper wrapper)
        {
            var routeTable = wrapper as RouteTableWrapper;

            var subnets = this.EC2Client.DescribeSubnets(new DescribeSubnetsRequest()).Subnets;
            var request = new DescribeRouteTablesRequest() { RouteTableIds = new List<string>() { routeTable.RouteTableId } };
            var response = this.EC2Client.DescribeRouteTables(request);
            var refreshedTable = response.RouteTables.FirstOrDefault(x => x.RouteTableId == routeTable.RouteTableId);

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                routeTable.Associations.Clear();
                if (refreshedTable != null)
                {
                    foreach (var item in refreshedTable.Associations)
                    {
                        if (string.IsNullOrEmpty(item.SubnetId))
                            continue;

                        var subnet = subnets.FirstOrDefault(x => x.SubnetId == item.SubnetId);
                        routeTable.Associations.Add(new RouteTableAssociationWrapper(item, subnet));
                    }
                }
            }));
        }

        public void RefreshRouteTables()
        {
            var subnets = this.EC2Client.DescribeSubnets(new DescribeSubnetsRequest()).Subnets;
            var response = this.EC2Client.DescribeRouteTables(new DescribeRouteTablesRequest());
            
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.RouteTables.Clear();
                foreach (var item in response.RouteTables)
                {
                    this.Model.RouteTables.Add(new RouteTableWrapper(item, subnets));
                }
            }));
        }

        public RouteTableWrapper CreateRouteTable()
        {
            var controller = new CreateRouteTableController();
            var result = controller.Execute(this.EC2Client);
            if (result.Success)
            {
                RefreshRouteTables();
                foreach (var item in Model.RouteTables)
                {
                    if (result.FocalName.Equals(item.RouteTableId))
                        return item;
                }
            }
            return null;
        }

        public void DeleteRouteTable(RouteTableWrapper routeTable)
        {
            var request = new DeleteRouteTableRequest() { RouteTableId = routeTable.NativeRouteTable.RouteTableId };
            this.EC2Client.DeleteRouteTable(request);
        }
    }
}
