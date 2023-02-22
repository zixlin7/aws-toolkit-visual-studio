using System.Collections.Generic;
using System.Text;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Util;
using Amazon.RDS;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class DeleteSubnetGroupController : BaseContextCommand
    {
        RDSSubnetGroupsRootViewModel _subnetGroupsRootViewModel;
        private readonly ToolkitContext _toolkitContext;

        public DeleteSubnetGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public DeleteSubnetGroupController(ToolkitContext toolkitContext, RDSSubnetGroupsRootViewModel subnetGroupsRootViewModel) : this(toolkitContext)
        {
            _subnetGroupsRootViewModel = subnetGroupsRootViewModel;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteSubnetGroup(model);
            RecordMetric(result);
            return result;
        }

        private ActionResults DeleteSubnetGroup(IViewModel model)
        {
            var rdsSubnetGroupViewModel = model as RDSSubnetGroupViewModel;
            if (rdsSubnetGroupViewModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find RDS Subnet group data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            return Execute(rdsSubnetGroupViewModel.RDSClient, 
                           rdsSubnetGroupViewModel.Parent as RDSSubnetGroupsRootViewModel, 
                           new List<DBSubnetGroupWrapper>{rdsSubnetGroupViewModel.SubnetGroup});
        }

        public ActionResults Execute(IAmazonRDS rdsClient, RDSSubnetGroupsRootViewModel subnetGroupsRootViewModel, IList<DBSubnetGroupWrapper> groups)
        {

            _subnetGroupsRootViewModel = subnetGroupsRootViewModel;
            string msg;
            if (groups.Count == 1)
            {
                var dbSubnetGroup = groups[0].NativeSubnetGroup;
                msg = $"Are you sure you want to delete the subnet group '{dbSubnetGroup.DBSubnetGroupName}' (associated with VPC '{dbSubnetGroup.VpcId}')?";
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

            if (!_toolkitContext.ToolkitHost.Confirm("Delete Subnet Group(s)", msg))
            {
                return ActionResults.CreateCancelled();
            }

            var failureCount = 0;
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
                    {
                        subnetGroupsRootViewModel.RemoveDBSubnetGroup(groupName);
                    }
                }
                catch (AmazonRDSException e)
                {
                    var errMsg = $"An error occurred attempting to delete the subnet group {groupName}: {e.Message}";
                    _toolkitContext.ToolkitHost.ShowError("Subnet Group Deletion Failed", errMsg);
                    failureCount++;
                }
            }

            // if deletes for all groups failed, report a failure result
            if (failureCount > 0 && failureCount == groups.Count)
            {
                return ActionResults.CreateFailed();
            }

            return new ActionResults().WithSuccess(true);
        }

        private void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _subnetGroupsRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsDeleteSubnetGroup(1, results, awsConnectionSettings);
        }
    }
}
