using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRoleViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnEdit
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            IAMRoleViewModel roleModel = focus as IAMRoleViewModel;
            roleModel.IAMRoleRootViewModel.RemoveRole(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Edit", OnEdit, null, true, this.GetType().Assembly, "IdentityManagement.user-service-root.png"),
                new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
            );
    }
}
