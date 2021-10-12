using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMGroupRootViewMetaNode : AbstractMetaNode
    {
        public IAMGroupViewMetaNode IAMGroupViewMetaNode => this.FindChild<IAMGroupViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnCreateGroup
        {
            get;
            set;
        }

        public void OnCreateGroupResponse(IViewModel focus, ActionResults results)
        {
            IAMGroupRootViewModel rootModel = focus as IAMGroupRootViewModel;
            object group;
            if (results.Parameters.TryGetValue(IAMActionResultsConstants.PARAM_IAM_GROUP, out group) && group is Group)
            {
                rootModel.AddGroup((Group)group);
            }
        }

        public override IList<ActionHandlerWrapper> Actions => BuildActionHandlerList(new ActionHandlerWrapper("Create Group...", OnCreateGroup, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateGroupResponse), false, typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.IamUserGroup.Path));
    }
}
