using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class RepoViewMetaNode : AbstractMetaNode, IRepoViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler GetRepoEndpoint
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Copy NuGet Source Endpoint", GetRepoEndpoint, null, true, null, null)
            );
    }
}
