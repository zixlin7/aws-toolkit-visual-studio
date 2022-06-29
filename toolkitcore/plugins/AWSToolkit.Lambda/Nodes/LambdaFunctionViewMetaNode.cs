using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
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

        public ActionHandlerWrapper.ActionHandler OnViewLogs
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            var functionModel = focus as LambdaFunctionViewModel;
            functionModel.LambdaRootViewModel.RemoveFunction(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View Function", OnOpen, null, true, typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.Lambda.Path),
                new ActionHandlerWrapper("View Logs",
                    OnViewLogs,
                    null,
                    false,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.CloudWatchLogGroups.Path),
                null,
                new ActionHandlerWrapper("Delete", OnDelete,
                    new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
            );
    }
}
