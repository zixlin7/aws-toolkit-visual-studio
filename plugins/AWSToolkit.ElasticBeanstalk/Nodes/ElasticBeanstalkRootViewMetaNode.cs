using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ElasticBeanstalkRootViewMetaNode : ServiceRootViewMetaNode, IElasticBeanstalkRootViewMetaNode
    {
        public const string BEANSTALK_ENDPOINT_LOOKUP = "ElasticBeanstalk";

        public ApplicationViewMetaNode ApplicationViewMetaNode
        {
            get { return this.FindChild<ApplicationViewMetaNode>(); }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new ElasticBeanstalkRootViewModel(account);
        }

        public override string EndPointSystemName
        {
            get { return BEANSTALK_ENDPOINT_LOOKUP; }
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/elasticbeanstalk/";
            }
        }
    }
}
