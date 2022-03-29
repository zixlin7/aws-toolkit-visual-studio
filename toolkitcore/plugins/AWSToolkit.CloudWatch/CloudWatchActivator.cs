using System;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Nodes;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CloudWatch
{
    /// <summary>
    /// Registers and activates the Amazon CloudWatch plugin in the toolkit
    /// </summary>
    public class CloudWatchActivator : AbstractPluginActivator
    {
        public override string PluginName => "CloudWatch";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new CloudWatchRootViewMetaNode(ToolkitContext);
            var logGroupsMetaNode = new LogGroupsRootViewMetaNode();
            rootMetaNode.Children.Add(logGroupsMetaNode);

            SetupContextHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IRepositoryFactory))
            {
                return new RepositoryFactory(ToolkitContext);
            }

            return null;
        }

        private void SetupContextHooks(CloudWatchRootViewMetaNode rootNode)
        {
            rootNode.FindChild<LogGroupsRootViewMetaNode>().OnView =
                new ContextCommandExecutor(() => new ViewLogGroupsCommand(ToolkitContext)).Execute;
        }
    }
}
