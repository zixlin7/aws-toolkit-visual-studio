using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMGroupViewMetaNode : AbstractMetaNode
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
            IAMGroupViewModel groupModel = focus as IAMGroupViewModel;
            groupModel.IAMGroupRootViewModel.RemoveGroup(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Edit", OnEdit, null, true, this.GetType().Assembly, "IdentityManagement.group-service-root.png"),
                    new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
                    );
            }
        }

    }
}
