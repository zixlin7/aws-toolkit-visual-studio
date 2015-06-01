using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public class CloudFormationStackViewMetaNode : AbstractMetaNode, ICloudFormationStackViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnOpen
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnCreateConfig
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            var stackModel = focus as CloudFormationStackViewModel;
            stackModel.CloudFormationRootViewModel.RemoveStack(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Open", OnOpen, null, true, null, null),
                    new ActionHandlerWrapper("Save Configuration", OnCreateConfig, null, false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.import.png"),
                    null,
                    new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.delete_stack.png")
                    );
            }
        }

    }
}
