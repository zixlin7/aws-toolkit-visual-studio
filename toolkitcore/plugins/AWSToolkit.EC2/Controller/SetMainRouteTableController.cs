using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class SetMainRouteTableController
    {
        public ActionResults Execute(IAmazonEC2 ec2Client, RouteTableWrapper newMainRoute)
        {
            var describeRoutesRequest = new DescribeRouteTablesRequest() 
            { 
                Filters = new List<Filter>() 
                { 
                    new Filter() { Name = "vpc-id", Values = new List<string>(){newMainRoute.VpcId} } 
                } 
            };

            var response = ec2Client.DescribeRouteTables(describeRoutesRequest);

            RouteTable currentMainTable = null;
            RouteTableAssociation currentMainAssociation = null;
            foreach (var table in response.RouteTables)
            {
                currentMainAssociation = table.Associations.FirstOrDefault(x => x.Main);
                if (currentMainAssociation != null)
                {
                    currentMainTable = table;
                    break;
                }
            }

            if (currentMainTable == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Set Main Error", string.Format("Failed to find current main route table for VPC.", newMainRoute.VpcId));
                return new ActionResults() { Success = false };
            }
            else if (currentMainTable.RouteTableId == newMainRoute.RouteTableId)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Set Main Error", string.Format("Route table {0} is already set as the main route table.", currentMainTable.RouteTableId));
                return new ActionResults() { Success = false };
            }

            var replaceRequest = new ReplaceRouteTableAssociationRequest()
            {
                AssociationId = currentMainAssociation.RouteTableAssociationId, 
                RouteTableId = newMainRoute.RouteTableId
            };
            ec2Client.ReplaceRouteTableAssociation(replaceRequest);

            return new ActionResults() { Success = true, ShouldRefresh = true };
        }
    }
}
