using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.SQS.Controller;

namespace Amazon.AWSToolkit.SQS
{
    public class SQSActivator : AbstractPluginActivator
    {
        public override string PluginName => "SQS";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new SQSRootViewMetaNode();
            var queueMetaNode = new SQSQueueViewMetaNode();

            rootMetaNode.Children.Add(queueMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(SQSRootViewMetaNode sqsRootNode)
        {
            sqsRootNode.OnCreate =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateQueueCommand>().Execute);

            SQSQueueViewMetaNode queueMetaNode = sqsRootNode.FindChild<SQSQueueViewMetaNode>();
            queueMetaNode.OnView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<QueueViewCommand>().Execute);

            queueMetaNode.OnPermissions =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<PermissionsCommand>().Execute);

            queueMetaNode.OnEditPolicy =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<SQSPolicyEditorController>().Execute);

            queueMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteQueueCommand>().Execute);

        }
    }
}
