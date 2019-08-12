using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public class SimpleDBDomainViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnOpen
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnProperties
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            SimpleDBDomainViewModel bucketModel = focus as SimpleDBDomainViewModel;
            bucketModel.SimpleDBRootViewModel.RemoveDomain(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Open", OnOpen, null, true, this.GetType().Assembly, "Amazon.AWSToolkit.SimpleDB.Resources.EmbeddedImages.domain-node.png"),
                new ActionHandlerWrapper("Properties...", OnProperties, null, false, null, "properties.png"),
                null,
                new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
            );
    }
}
