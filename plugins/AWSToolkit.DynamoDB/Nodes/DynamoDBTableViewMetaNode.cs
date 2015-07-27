using System.Collections.Generic;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class DynamoDBTableViewMetaNode : AbstractMetaNode
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

        public ActionHandlerWrapper.ActionHandler OnStreamProperties
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            DynamoDBTableViewModel tableModel = focus as DynamoDBTableViewModel;
            tableModel.DynamoDBRootViewModel.RemoveTable(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Open", OnOpen, null, true, this.GetType().Assembly, "Amazon.AWSToolkit.DynamoDB.Resources.EmbeddedImages.table.png"),
                    new ActionHandlerWrapper("Stream Properties...", OnProperties, null, false, null, null),
                    new ActionHandlerWrapper("Table Properties...", OnProperties, null, false, null, "properties.png"),
                    null,
                    new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
                    );
            }
        }


    }
}
