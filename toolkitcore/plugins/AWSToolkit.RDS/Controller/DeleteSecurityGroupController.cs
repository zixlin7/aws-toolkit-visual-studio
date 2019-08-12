using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.EC2.Controller;

using Amazon.RDS;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class DeleteSecurityGroupController : BulkChangeController<IAmazonRDS, DBSecurityGroupWrapper>
    {
        RDSSecurityGroupRootViewModel _securityGroupRootViewModel;

        public DeleteSecurityGroupController()
        {
        }

        public DeleteSecurityGroupController(RDSSecurityGroupRootViewModel securityGroupRootViewModel)
        {
            this._securityGroupRootViewModel = securityGroupRootViewModel;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var rdsSecurityViewModel = model as RDSSecurityGroupViewModel;
            if (rdsSecurityViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._securityGroupRootViewModel = rdsSecurityViewModel.Parent as RDSSecurityGroupRootViewModel;
            var list = new List<DBSecurityGroupWrapper>() { rdsSecurityViewModel.DBGroup };
            return base.Execute(rdsSecurityViewModel.RDSClient, list);
        }

        protected override string Action => "Delete";

        protected override string ConfirmMessage => "Are you sure you want to delete the security group(s)";

        protected override void PerformAction(IAmazonRDS rdsClient, DBSecurityGroupWrapper group)
        {
            rdsClient.DeleteDBSecurityGroup(new DeleteDBSecurityGroupRequest() { DBSecurityGroupName = group.DisplayName });

            if (this._securityGroupRootViewModel != null)
                this._securityGroupRootViewModel.RemoveSecurityGroup(group.DisplayName);
        }
    }
}
