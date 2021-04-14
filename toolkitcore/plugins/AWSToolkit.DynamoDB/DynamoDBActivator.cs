using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.Controller;
using Amazon.DynamoDBv2;


namespace Amazon.AWSToolkit.DynamoDB
{
    public class DynamoDBActivator : AbstractPluginActivator
    {
        public override string PluginName => "DynamoDB";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new DynamoDBRootViewMetaNode(ToolkitContext);
            var domainMetaNode = new DynamoDBTableViewMetaNode();

            rootMetaNode.Children.Add(domainMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);

            this.ToolkitContext.RegionProvider.SetLocalEndpoint(DynamoDBConstants.ServiceNames.DynamoDb, "http://localhost:8000");
        }

        void setupContextMenuHooks(DynamoDBRootViewMetaNode rootNode)
        {
            rootNode.OnTableCreate =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new CreateTableController(ToolkitContext)).Execute);

            rootNode.OnStartLocal = 
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<StartLocalDynamoDBController>().Execute);
            rootNode.OnStopLocal = 
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<StopLocalDynamoDBController>().Execute);

            rootNode.DynamoDBTableViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteTableController>().Execute);

            rootNode.DynamoDBTableViewMetaNode.OnProperties =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<TablePropertiesController>().Execute);

            rootNode.DynamoDBTableViewMetaNode.OnStreamProperties =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<StreamPropertiesController>().Execute);

            rootNode.DynamoDBTableViewMetaNode.OnOpen =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<TableBrowserController>().Execute);
        }
    }
}
