using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontRootViewMetaNode : ServiceRootViewMetaNode
    {        
        public const string CLOUDFRONT_ENDPOINT_LOOKUP = "CloudFront";

        public CloudFrontDistributeViewMetaNode CloudFrontDistributeViewMetaNode
        {
            get { return this.FindChild<CloudFrontDistributeViewMetaNode>(); }
        }

        public CloudFrontStreamingDistributeViewMetaNode CloudFrontStreamingDistributeViewMetaNode
        {
            get { return this.FindChild<CloudFrontStreamingDistributeViewMetaNode>(); }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new CloudFrontRootViewModel(account);
        }

        public override string EndPointSystemName
        {
            get { return CLOUDFRONT_ENDPOINT_LOOKUP; }
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

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Create Distribution...", OnCreateDistribution, null, false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.CloudFront.Resources.EmbeddedImages.distribution.png"),
                    new ActionHandlerWrapper("Create Streaming Distribution...", OnCreateStreamingDistribution, null, false, 
                        this.GetType().Assembly, "Amazon.AWSToolkit.CloudFront.Resources.EmbeddedImages.streaming-distribution.png"),
                    new ActionHandlerWrapper("View Origin Access Identities...", OnViewOriginAccessIdentities, null, false, null, null));
            }
        }


        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/cloudfront/";
            }
        }
    }
}
