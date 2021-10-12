using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;
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

        static ILog _logger = LogManager.GetLogger(typeof(RootViewMetaNode));

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
                _logger.InfoFormat("Region {0} has no ECS endpoint", region.Id);
            }

            var ecrServiceAvailable = regionProvider.IsServiceAvailable(EcrServiceName, region.Id);
            if (!ecrServiceAvailable)
            {
                _logger.InfoFormat("Region {0} has no ECR endpoint", region.Id);
            }
            return ecsServiceAvailable && ecrServiceAvailable;
        }
        public ActionHandlerWrapper.ActionHandler OnLaunch
        {
            get;
            set;
        }

        public void OnLaunchResponse(IViewModel focus, ActionResults results)
        {
            RootViewModel rootModel = focus as RootViewModel;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(new ActionHandlerWrapper("Launch cluster...", 
                OnLaunch, 
                this.OnLaunchResponse, 
                false,
                typeof(AwsImageResourcePath).Assembly,
                AwsImageResourcePath.ElasticContainerServiceCluster.Path));

        public override string MarketingWebSite => "https://aws.amazon.com/ecs/";
    }
}
