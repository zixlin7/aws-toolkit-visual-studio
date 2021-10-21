using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
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
                new ActionHandlerWrapper("View Status", OnEnvironmentStatus, null, true, typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.ElasticBeanstalkEnvironment.Path),
                null,
                new ActionHandlerWrapper("Restart App", OnRestartApp, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.restart.png"),
                new ActionHandlerWrapper("Rebuild Environment", OnRebuildingEnvironment, null, false, null, null),
                null,
                new ActionHandlerWrapper("Terminate Environment", OnTerminateEnvironment, null, false, null, "delete.png")
            );
    }
}
