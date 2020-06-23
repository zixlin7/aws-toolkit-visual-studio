using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class EnvironmentViewMetaNode : AbstractMetaNode, IEnvironmentViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnEnvironmentStatus
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnRestartApp
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnTerminateEnvironment
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnRebuildingEnvironment
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View Status", OnEnvironmentStatus, null, true, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.environment_view.png"),
                null,
                new ActionHandlerWrapper("Restart App", OnRestartApp, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.restart.png"),
                new ActionHandlerWrapper("Rebuild Environment", OnRebuildingEnvironment, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.rebuild.png"),
                null,
                new ActionHandlerWrapper("Terminate Environment", OnTerminateEnvironment, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.terminate.png")
            );
    }
}
