using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ElasticBeanstalkRootViewMetaNode : ServiceRootViewMetaNode, IElasticBeanstalkRootViewMetaNode
    {
        public const string BEANSTALK_ENDPOINT_LOOKUP = "ElasticBeanstalk";

        public ApplicationViewMetaNode ApplicationViewMetaNode => this.FindChild<ApplicationViewMetaNode>();

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new ElasticBeanstalkRootViewModel(account);
        }

        public override string EndPointSystemName => BEANSTALK_ENDPOINT_LOOKUP;

        public override string MarketingWebSite => "http://aws.amazon.com/elasticbeanstalk/";
    }
}
