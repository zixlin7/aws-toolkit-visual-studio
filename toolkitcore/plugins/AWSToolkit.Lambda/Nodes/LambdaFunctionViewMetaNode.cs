using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public class LambdaFunctionViewMetaNode : AbstractMetaNode
    {

        public ActionHandlerWrapper.ActionHandler OnOpen
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
            var functionModel = focus as LambdaFunctionViewModel;
            functionModel.LambdaRootViewModel.RemoveFunction(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View Function", OnOpen, null, true, null, null),
                    null,
                    new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
                    );
            }
        }

    }
}
