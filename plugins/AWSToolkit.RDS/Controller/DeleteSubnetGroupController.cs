using System.Collections.Generic;
using System.Text;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.RDS;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class DeleteSubnetGroupController : BaseContextCommand
    {
        RDSSubnetGroupsRootViewModel _subnetGroupsRootViewModel;

        public DeleteSubnetGroupController()
        {
        }

        public DeleteSubnetGroupController(RDSSubnetGroupsRootViewModel subnetGroupsRootViewModel)
        {
            this._subnetGroupsRootViewModel = subnetGroupsRootViewModel;
        }


        public override ActionResults Execute(IViewModel model)
        {
            var rdsSubnetGroupViewModel = model as RDSSubnetGroupViewModel;
            if (rdsSubnetGroupViewModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(rdsSubnetGroupViewModel.RDSClient, 
                           rdsSubnetGroupViewModel.Parent as RDSSubnetGroupsRootViewModel, 
                           new List<DBSubnetGroupWrapper>{rdsSubnetGroupViewModel.SubnetGroup});
        }

        public ActionResults Execute(IAmazonRDS rdsClient, RDSSubnetGroupsRootViewModel subnetGroupsRootViewModel, IList<DBSubnetGroupWrapper> groups)
        {
            string msg;
            if (groups.Count == 1)
            {
                var dbSubnetGroup = groups[0].NativeSubnetGroup;
                msg = string.Format("Are you sure you want to delete the subnet group '{0}' (associated with VPC '{1}')?",
                                    dbSubnetGroup.DBSubnetGroupName,
                                    dbSubnetGroup.VpcId);
            }
            else
            {
                var sb = new StringBuilder("Are you sure you want to delete the subnet groups ");
                for (var i = 0; i < groups.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(groups[i].Name);
                }

                sb.Append("?");
                msg = sb.ToString();
            }

            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Subnet Group(s)", msg))
            {
                foreach (var group in groups)
                {
                    var groupName = group.NativeSubnetGroup.DBSubnetGroupName;
                    try
                    {
                        rdsClient.DeleteDBSubnetGroup(new DeleteDBSubnetGroupRequest
                        {
                            DBSubnetGroupName = groupName
                        });

                        if (subnetGroupsRootViewModel != null)
                            subnetGroupsRootViewModel.RemoveDBSubnetGroup(groupName);
                    }
                    catch (AmazonRDSException e)
                    {
                        var errMsg = string.Format("An error occurred attempting to delete the subnet group {0}: {1}", groupName, e.Message);
                        ToolkitFactory.Instance.ShellProvider.ShowError("Subnet Group Deletion Failed", errMsg);
                    }

                }
            }

            return new ActionResults().WithSuccess(true);
        }
    }
}
