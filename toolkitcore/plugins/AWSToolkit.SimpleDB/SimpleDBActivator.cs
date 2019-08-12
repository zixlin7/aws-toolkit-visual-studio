using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.SimpleDB.Nodes;
using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB
{
    public class SimpleDBActivator : AbstractPluginActivator
    {
        public override string PluginName => "SimpleDB";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new SimpleDBRootViewMetaNode();
            var domainMetaNode = new SimpleDBDomainViewMetaNode();

            rootMetaNode.Children.Add(domainMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(SimpleDBRootViewMetaNode rootNode)
        {
            rootNode.OnDomainCreate =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateDomainController>().Execute);

            rootNode.SimpleDBDomainViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteDomainController>().Execute);

            rootNode.SimpleDBDomainViewMetaNode.OnOpen =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<QueryBrowserController>().Execute);

            rootNode.SimpleDBDomainViewMetaNode.OnProperties =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DomainPropertiesController>().Execute);

        }
    }
}
