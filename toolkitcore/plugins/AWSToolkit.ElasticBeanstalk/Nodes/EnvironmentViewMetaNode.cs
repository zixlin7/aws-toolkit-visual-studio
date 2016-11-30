using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public ActionHandlerWrapper.ActionHandler OnCreateConfig
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View Status", OnEnvironmentStatus, null, true, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.environment_view.png"),
                    new ActionHandlerWrapper("Save Configuration", OnCreateConfig, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.import.png"),
                    null,
                    new ActionHandlerWrapper("Restart App", OnRestartApp, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.restart.png"),
                    new ActionHandlerWrapper("Rebuild Environment", OnRebuildingEnvironment, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.rebuild.png"),
                    null,
                    new ActionHandlerWrapper("Terminate Environment", OnTerminateEnvironment, null, false, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.terminate.png")
                    );
            }
        }

    }
}
