using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class TaskDefinitionsRootViewMetaNode : AbstractMetaNode
    {
        public TaskDefinitionViewMetaNode TaskDefinitionViewMetaNode
        {
            get { return this.FindChild<TaskDefinitionViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public ActionHandlerWrapper.ActionHandler OnCreateTaskDefinition
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Create Task Definition...",
                        OnCreateTaskDefinition,
                        null,
                        false,
                        this.GetType().Assembly,
                        "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.new_taskdef.png")
                );
            }
        }
    }
}
