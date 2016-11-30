using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.Controller;


namespace Amazon.AWSToolkit.DynamoDB
{
    public class DynamoDBActivator : AbstractPluginActivator
    {
        public override string PluginName
        {
            get { return "DynamoDB"; }
        }

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new DynamoDBRootViewMetaNode();
            var domainMetaNode = new DynamoDBTableViewMetaNode();

            rootMetaNode.Children.Add(domainMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);

            RegionEndPointsManager.Instance.LocalRegion.RegisterEndPoint(DynamoDBRootViewMetaNode.DYNAMODB_ENDPOINT_LOOKUP, "http://localhost:8000");
        }

        void setupContextMenuHooks(DynamoDBRootViewMetaNode rootNode)
        {
            rootNode.OnTableCreate =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateTableController>().Execute);

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
