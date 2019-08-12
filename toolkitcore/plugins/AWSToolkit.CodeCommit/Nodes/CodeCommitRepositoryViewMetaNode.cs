using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRepositoryViewMetaNode : AbstractMetaNode, ICodeCommitRepositoryViewMetaNode
    {

        public ActionHandlerWrapper.ActionHandler OnOpenRepositoryView
        {
            get;
            set;
        }


        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList
            (
                new ActionHandlerWrapper("Open", OnOpenRepositoryView, null, true, null, null)
            );
    }
}