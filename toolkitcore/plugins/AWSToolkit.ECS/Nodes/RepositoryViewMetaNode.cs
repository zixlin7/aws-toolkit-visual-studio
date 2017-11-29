using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RepositoryViewMetaNode : FeatureViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View",
                        OnView,
                        null,
                        true,
                        this.GetType().Assembly,
                        "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.repository.png"),
                null,
                new ActionHandlerWrapper("Delete",
                        OnDelete,
                        null,
                        false,
                        this.GetType().Assembly,
                        "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.delete-repository.png")
                );
            }
        }
    }
}
