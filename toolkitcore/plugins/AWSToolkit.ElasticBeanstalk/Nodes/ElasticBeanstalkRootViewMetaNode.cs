using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.ElasticBeanstalk;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ElasticBeanstalkRootViewMetaNode : ServiceRootViewMetaNode, IElasticBeanstalkRootViewMetaNode
    {

        private static readonly string EBSServiceName = new AmazonElasticBeanstalkConfig().RegionEndpointServiceName;

        public ApplicationViewMetaNode ApplicationViewMetaNode => this.FindChild<ApplicationViewMetaNode>();

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new ElasticBeanstalkRootViewModel(account, region);
        }

        public override string SdkEndpointServiceName => EBSServiceName;

        public override string MarketingWebSite => "https://aws.amazon.com/elasticbeanstalk/";
    }
}
