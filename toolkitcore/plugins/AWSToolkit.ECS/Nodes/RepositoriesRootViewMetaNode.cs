using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RepositoriesRootViewMetaNode : AbstractMetaNode
    {

        public RepositoryViewMetaNode RepositoryViewMetaNode
        {
            get { return this.FindChild<RepositoryViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public ActionHandlerWrapper.ActionHandler OnCreateRepository
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Create Repository...",
                        OnCreateRepository,
                        null,
                        false,
                        this.GetType().Assembly,
                        "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.new_repository.png")
                );
            }
        }
    }
}
