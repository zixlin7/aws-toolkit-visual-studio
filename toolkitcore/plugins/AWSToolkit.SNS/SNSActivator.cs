using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.Controller;


namespace Amazon.AWSToolkit.SNS
{
    public class SNSActivator : AbstractPluginActivator
    {
        public override string PluginName => "SNS";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new SNSRootViewMetaNode();
            var topicMetaNode = new SNSTopicViewMetaNode();

            rootMetaNode.Children.Add(topicMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(SNSRootViewMetaNode rootNode)
        {
            rootNode.OnCreateTopic =
                new ContextCommandExecutor(() => new CreateTopicController(ToolkitContext)).Execute;

            rootNode.OnViewSubscriptions =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewSubscriptionsController>().Execute);

            rootNode.SNSTopicViewMetaNode.OnViewTopic =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewTopicController>().Execute);

            rootNode.SNSTopicViewMetaNode.OnEditPolicy =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<SNSPolicyEditorController>().Execute);

            rootNode.SNSTopicViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new DeleteTopicController(ToolkitContext)).Execute);
        }
    }
}
