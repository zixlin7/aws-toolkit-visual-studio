using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMUserViewMetaNode : AbstractMetaNode
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
            IAMUserViewModel userModel = focus as IAMUserViewModel;
            userModel.IAMUserRootViewModel.RemoveUser(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Edit", OnEdit, null, true, this.GetType().Assembly, "IdentityManagement.user-service-root.png"),
                new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
            );
    }
}
