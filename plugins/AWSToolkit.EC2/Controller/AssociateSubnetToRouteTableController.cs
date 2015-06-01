using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AssociateSubnetToRouteTableController : IAssociateSubnetController
    {
        ActionResults _results;
        AssociateSubnetModel _model;
        IAmazonEC2 _ec2Client;
        RouteTableWrapper _routeTable;

        public ActionResults Execute(IAmazonEC2 ec2Client, RouteTableWrapper routeTable)
        {
            this._ec2Client = ec2Client;
            this._routeTable = routeTable;
            this._model = new AssociateSubnetModel();
            this._model.AvailableSubnets = this.GetAvailableSubnets();

            if (this.Model.AvailableSubnets.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("There are no subnets unassociated with any route tables.");
                return new ActionResults().WithSuccess(false);
            }
            this._model.SelectedSubnet = this._model.AvailableSubnets[0];

            var control = new AssociateSubnetControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public AssociateSubnetModel Model
        {
            get { return this._model; }
        }

        public IList<SubnetWrapper> GetAvailableSubnets()
        {
            var subnets = new List<SubnetWrapper>();

            var ec2Subnets = this._ec2Client.DescribeSubnets(new DescribeSubnetsRequest()).Subnets;
            var ec2RouteTables = this._ec2Client.DescribeRouteTables(new DescribeRouteTablesRequest()).RouteTables;
            HashSet<string> subnetsAlreadyAssociated = new HashSet<string>();
            foreach (var route in ec2RouteTables)
            {
                foreach (var association in route.Associations)
                {
                    if(!string.IsNullOrEmpty(association.SubnetId) && !subnetsAlreadyAssociated.Contains(association.SubnetId))
                        subnetsAlreadyAssociated.Add(association.SubnetId);
                }
            }

            foreach (var subnet in ec2Subnets)
            {
                if (!string.Equals(subnet.VpcId, this._routeTable.VpcId) || subnetsAlreadyAssociated.Contains(subnet.SubnetId))
                    continue;

                subnets.Add(new SubnetWrapper(subnet, null, null));
            }

            return subnets;
        }

        public void AssociateSubnet()
        {
            var request = new AssociateRouteTableRequest() { RouteTableId = this._routeTable.RouteTableId, SubnetId = this.Model.SelectedSubnet.SubnetId };
            this._ec2Client.AssociateRouteTable(request);
            this._results = new ActionResults() { Success = true, FocalName = this.Model.SelectedSubnet.SubnetId };
        }
    }
}
