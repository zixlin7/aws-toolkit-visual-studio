using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class TaskDefinitionsRootViewMetaNode : AbstractMetaNode
    {
        public TaskDefinitionViewMetaNode TaskDefinitionViewMetaNode => this.FindChild<TaskDefinitionViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnCreateTaskDefinition
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create Task Definition...",
                    OnCreateTaskDefinition,
                    null,
                    false,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.EcsTaskDefinition.Path)
            );
    }
}
