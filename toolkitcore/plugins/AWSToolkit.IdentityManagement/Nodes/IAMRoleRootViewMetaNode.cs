using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRoleRootViewMetaNode : AbstractMetaNode
    {
        public IAMRoleViewMetaNode IAMRoleViewMetaNode => this.FindChild<IAMRoleViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnCreateRole
        {
            get;
            set;
        }

        public void OnCreateRoleResponse(IViewModel focus, ActionResults results)
        {
            IAMRoleRootViewModel rootModel = focus as IAMRoleRootViewModel;
            object role;
            if (results.Parameters.TryGetValue(IAMActionResultsConstants.PARAM_IAM_ROLE, out role) && role is Role)
            {
                rootModel.AddRole((Role)role);
            }
        }

        public override IList<ActionHandlerWrapper> Actions => BuildActionHandlerList(new ActionHandlerWrapper("Create Role...", OnCreateRole, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateRoleResponse), false, typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.IamRole.Path));
    }
}
