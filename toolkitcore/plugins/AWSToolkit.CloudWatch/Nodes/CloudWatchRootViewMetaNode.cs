using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Nodes
{
    /// <summary>
    /// Service node for Amazon CloudWatch
    /// </summary>
    public class CloudWatchRootViewMetaNode : ServiceRootViewMetaNode
    {
        static ILog Logger = LogManager.GetLogger(typeof(CloudWatchRootViewMetaNode));

        private const bool IsEnabled = false;
        private readonly ToolkitContext _toolkitContext;

        private static readonly string CloudWatchServiceName = new AmazonCloudWatchConfig().RegionEndpointServiceName;

        private static readonly string CloudWatchLogsServiceName =
            new AmazonCloudWatchLogsConfig().RegionEndpointServiceName;

        public CloudWatchRootViewMetaNode(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override string SdkEndpointServiceName => CloudWatchServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new CloudWatchRootViewModel(account.MetaNode.FindChild<CloudWatchRootViewMetaNode>(), account, account.Identifier,
                region, _toolkitContext);

        }

        public override bool CanSupportRegion(ToolkitRegion region, IRegionProvider regionProvider)
        {
            // TODO: Remove flag when ready to enable this service in the explorer
            if (!IsEnabled)
            {
                return false;
            }

            var cloudWatchServiceAvailable = regionProvider.IsServiceAvailable(CloudWatchServiceName, region.Id);

            if (!cloudWatchServiceAvailable)
            {
                Logger.InfoFormat("Region {0} has no CloudWatch endpoint", region.Id);
            }

            var cloudWatchLogsServiceAvailable =
                regionProvider.IsServiceAvailable(CloudWatchLogsServiceName, region.Id);
            if (!cloudWatchServiceAvailable)
            {
                Logger.InfoFormat("Region {0} has no Cloudwatch Logs endpoint", region.Id);
            }

            return cloudWatchServiceAvailable && cloudWatchLogsServiceAvailable;


        }

        public override string MarketingWebSite => "https://aws.amazon.com/cloudwatch/";

        public ActionHandlerWrapper.ActionHandler OnViewLogGroups { get; set; }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View Log Groups",
                    OnViewLogGroups,
                    null,
                    true,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.CloudWatchLogGroups.Path)
            );
    }
}
