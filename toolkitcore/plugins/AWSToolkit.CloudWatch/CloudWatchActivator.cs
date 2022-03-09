using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudWatch.Nodes;

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

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }
    }
}
