using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RepositoriesRootViewMetaNode : AbstractMetaNode
    {

        public RepositoryViewMetaNode RepositoryViewMetaNode => this.FindChild<RepositoryViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnCreateRepository
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create Repository...",
                    OnCreateRepository,
                    null,
                    false,
                    this.GetType().Assembly,
                    "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.new_repository.png")
            );
    }
}
