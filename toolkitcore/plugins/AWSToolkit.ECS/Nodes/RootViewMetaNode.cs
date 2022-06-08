using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;
using Amazon.ECS;

using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string EcsServiceName = new AmazonECSConfig().RegionEndpointServiceName;
        private static readonly string EcrServiceName = new AmazonECRConfig().RegionEndpointServiceName;

        static readonly ILog Logger = LogManager.GetLogger(typeof(RootViewMetaNode));

        public override string SdkEndpointServiceName => EcsServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new RootViewModel(account, region);
        }

        public override bool CanSupportRegion(ToolkitRegion region, IRegionProvider regionProvider)
        {
            var ecsServiceAvailable = regionProvider.IsServiceAvailable(EcsServiceName, region.Id);

            if (!ecsServiceAvailable)
            {
                Logger.InfoFormat("Region {0} has no ECS endpoint", region.Id);
            }

            var ecrServiceAvailable = regionProvider.IsServiceAvailable(EcrServiceName, region.Id);
            if (!ecrServiceAvailable)
            {
                Logger.InfoFormat("Region {0} has no ECR endpoint", region.Id);
            }
            return ecsServiceAvailable && ecrServiceAvailable;
        }

        public override string MarketingWebSite => "https://aws.amazon.com/ecs/";
    }
}
