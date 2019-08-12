using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewSubnetsController : FeatureController<ViewSubnetsModel>
    {
        ViewSubnetsControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewSubnetsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshSubnets();
        }


        public void RefreshSubnets()
        {
            var subnetResponse = this.EC2Client.DescribeSubnets(new DescribeSubnetsRequest());

            var routeTableResponse = this.EC2Client.DescribeRouteTables(new DescribeRouteTablesRequest());
            var routeTableSubnetMap = new Dictionary<string, RouteTable>();
            var routeTableVpcMap = new Dictionary<string, RouteTable>();
            foreach (var rt in routeTableResponse.RouteTables)
            {
                foreach (var association in rt.Associations)
                {
                    if(association.Main)
                        routeTableVpcMap[rt.VpcId] = rt;

                    if(!string.IsNullOrEmpty(association.SubnetId))
                        routeTableSubnetMap[association.SubnetId] = rt;
                }
            }

            var networkAclResponse = this.EC2Client.DescribeNetworkAcls(new DescribeNetworkAclsRequest());
            var networkAclSubnetMap = new Dictionary<string, NetworkAcl>();
            NetworkAcl defaultAcl = null;
            foreach (var acl in networkAclResponse.NetworkAcls)
            {
                if (acl.IsDefault)
                    defaultAcl = acl;

                foreach (var association in acl.Associations)
                {
                    networkAclSubnetMap[association.SubnetId] = acl;
                }
            }

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this.Model.Subnets.Clear();
                foreach (var item in subnetResponse.Subnets.OrderBy(x => x.SubnetId))
                {
                    RouteTable routeTable = null;
                    routeTableSubnetMap.TryGetValue(item.SubnetId, out routeTable);
                    if(routeTable == null)
                        routeTableVpcMap.TryGetValue(item.VpcId, out routeTable);

                    NetworkAcl acl = null;
                    networkAclSubnetMap.TryGetValue(item.SubnetId, out acl);
                    if (acl == null)
                        acl = defaultAcl;

                    this.Model.Subnets.Add(new SubnetWrapper(item, routeTable, acl));
                }
            }));
        }

        public SubnetWrapper CreateSubnet()
        {
            var controller = new CreateSubnetController();
            var result = controller.Execute(this.EC2Client);
            if (result.Success)
            {
                RefreshSubnets();
                foreach (var item in Model.Subnets)
                {
                    if (result.FocalName.Equals(item.SubnetId))
                        return item;
                }
            }
            return null;
        }

        public void DeleteSubnet(SubnetWrapper item)
        {
            var request = new DeleteSubnetRequest { SubnetId = item.SubnetId };
            this.EC2Client.DeleteSubnet(request);
            this.Model.Subnets.Remove(item);
        }
    }
}
