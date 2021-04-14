using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudFront;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontRootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string CloudFrontServiceName = new AmazonCloudFrontConfig().RegionEndpointServiceName;

        public override string SdkEndpointServiceName => CloudFrontServiceName;

        public CloudFrontDistributeViewMetaNode CloudFrontDistributeViewMetaNode => this.FindChild<CloudFrontDistributeViewMetaNode>();

        public CloudFrontStreamingDistributeViewMetaNode CloudFrontStreamingDistributeViewMetaNode => this.FindChild<CloudFrontStreamingDistributeViewMetaNode>();

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new CloudFrontRootViewModel(account, region);
        }

       
        public ActionHandlerWrapper.ActionHandler OnCreateDistribution
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnCreateStreamingDistribution
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnViewOriginAccessIdentities
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create Distribution...", OnCreateDistribution, null, false,
                    this.GetType().Assembly, "Amazon.AWSToolkit.CloudFront.Resources.EmbeddedImages.distribution.png"),
                new ActionHandlerWrapper("Create Streaming Distribution...", OnCreateStreamingDistribution, null, false, 
                    this.GetType().Assembly, "Amazon.AWSToolkit.CloudFront.Resources.EmbeddedImages.streaming-distribution.png"),
                new ActionHandlerWrapper("View Origin Access Identities...", OnViewOriginAccessIdentities, null, false, null, null));


        public override string MarketingWebSite => "https://aws.amazon.com/cloudfront/";
    }
}
