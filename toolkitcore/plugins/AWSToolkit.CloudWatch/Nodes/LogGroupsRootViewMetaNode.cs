using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudWatch.Nodes
{
    /// <summary>
    /// Resource node for Amazon CloudWatch Log Groups
    /// </summary>
    public class LogGroupsRootViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnView
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View",
                    OnView,
                    null,
                    true,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.CloudWatchLogGroups.Path)
            );
    }
}
