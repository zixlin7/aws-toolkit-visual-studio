using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string ECS_ENDPOINT_LOOKUP = RegionEndPointsManager.ECS_SERVICE_NAME;
        public const string ECR_ENDPOINT_LOOKUP = RegionEndPointsManager.ECR_SERVICE_NAME;
        static ILog _logger = LogManager.GetLogger(typeof(RootViewMetaNode));

        public override string EndPointSystemName => ECS_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new RootViewModel(account);
        }

        public override bool CanSupportRegion(RegionEndPointsManager.RegionEndPoints region)
        {
            var ecsEndpoint = region.GetEndpoint(ECS_ENDPOINT_LOOKUP);
            if (ecsEndpoint == null)
            {
                _logger.InfoFormat("Region {0} has no ECS endpoint", region.SystemName);
            }
            var ecrEndpoint = region.GetEndpoint(ECR_ENDPOINT_LOOKUP);
            if (ecrEndpoint == null)
            {
                _logger.InfoFormat("Region {0} has no ECR endpoint", region.SystemName);
            }
            return ecsEndpoint != null && ecrEndpoint != null;
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
                this.GetType().Assembly, 
                "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.launch-cluster.png"));

        public override string MarketingWebSite => "http://aws.amazon.com/ecs/";
    }
}
