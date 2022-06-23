using System;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Nodes;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch
{
    /// <summary>
    /// Registers and activates the Amazon CloudWatch plugin in the toolkit
    /// </summary>
    public class CloudWatchActivator : AbstractPluginActivator
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CloudWatchActivator));

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
            rootNode.OnViewLogGroups = OnViewLogGroupsFromRoot;
            rootNode.FindChild<LogGroupsRootViewMetaNode>().OnView = OnViewLogGroups;
        }

        private ActionResults OnViewLogGroupsFromRoot(IViewModel viewModel)
        {
            IConnectionContextCommand CreateCommand()
            {
                if (!(viewModel is CloudWatchRootViewModel rootModel))
                {
                    Logger.Error($"Did not receive {nameof(CloudWatchRootViewModel)} when trying to view CloudWatch Logs.");
                    Logger.Error($"Received: {viewModel?.GetType().Name ?? "null"}");

                    throw new InvalidOperationException("AWS Explorer command for viewing CloudWatch Logs was unable to get CloudWatch Logs node.");
                }

                var awsConnectionSetting = new AwsConnectionSettings(rootModel.Identifier, rootModel.Region);

                return new ViewLogGroupsCommand(AwsExplorerMetricSource.CloudWatchLogsNode, ToolkitContext, awsConnectionSetting);
            }

            return new ConnectionContextCommandExecutor(CreateCommand, ToolkitContext.ToolkitHost).Execute();
        }

        private ActionResults OnViewLogGroups(IViewModel viewModel)
        {
            IConnectionContextCommand CreateCommand()
            {
                if (!(viewModel is LogGroupsRootViewModel logGroups))
                {
                    Logger.Error($"Did not receive {nameof(LogGroupsRootViewModel)} when trying to view CloudWatch Logs.");
                    Logger.Error($"Received: {viewModel?.GetType().Name ?? "null"}");

                    throw new InvalidOperationException("AWS Explorer command for viewing CloudWatch Logs was unable to get CloudWatch Log Groups node.");
                }
                
                var awsConnectionSetting = new AwsConnectionSettings(logGroups.AccountViewModel?.Identifier, logGroups.Region);

                return new ViewLogGroupsCommand(AwsExplorerMetricSource.CloudWatchLogsNode, ToolkitContext, awsConnectionSetting);
            }

            return new ConnectionContextCommandExecutor(CreateCommand, ToolkitContext.ToolkitHost).Execute();

        }
    }
}
