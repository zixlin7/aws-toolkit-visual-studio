using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.EC2.Controller;

using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.RDS.Util;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class DeleteSecurityGroupController : BulkChangeController<IAmazonRDS, DBSecurityGroupWrapper>
    {
        RDSSecurityGroupRootViewModel _securityGroupRootViewModel;
        private readonly ToolkitContext _toolkitContext;

        public DeleteSecurityGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public DeleteSecurityGroupController(ToolkitContext toolkitContext, RDSSecurityGroupRootViewModel securityGroupRootViewModel) : this(toolkitContext)
        {
            _securityGroupRootViewModel = securityGroupRootViewModel;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteSecurityGroup(model);
            RecordMetric(result);
            return result;
        }

        private ActionResults DeleteSecurityGroup(IViewModel model)
        {
            var rdsSecurityViewModel = model as RDSSecurityGroupViewModel;
            if (rdsSecurityViewModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find RDS security group data",
                 ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            _securityGroupRootViewModel = rdsSecurityViewModel.Parent as RDSSecurityGroupRootViewModel;
            var list = new List<DBSecurityGroupWrapper>() { rdsSecurityViewModel.DBGroup };
            return base.Execute(rdsSecurityViewModel.RDSClient, list);
        }

        protected override string Action => "Delete";

        protected override string ConfirmMessage => "Are you sure you want to delete the security group(s)";

        protected override void PerformAction(IAmazonRDS rdsClient, DBSecurityGroupWrapper group)
        {
            rdsClient.DeleteDBSecurityGroup(new DeleteDBSecurityGroupRequest() { DBSecurityGroupName = group.DisplayName });

            if (_securityGroupRootViewModel != null)
            {
                _securityGroupRootViewModel.RemoveSecurityGroup(group.DisplayName);
            }
        }

        private void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _securityGroupRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsDeleteSecurityGroup(1, results, awsConnectionSettings);
        }
    }
}
