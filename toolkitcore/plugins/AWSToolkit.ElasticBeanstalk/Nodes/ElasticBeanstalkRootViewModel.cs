using System;
using System.Collections.Generic;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ElasticBeanstalkRootViewModel : ServiceRootViewModel, IElasticBeanstalkRootViewModel
    {
        private readonly ElasticBeanstalkRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonElasticBeanstalk> _beanstalkClient;

        public ElasticBeanstalkRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<ElasticBeanstalkRootViewMetaNode>(), accountViewModel, "AWS Elastic Beanstalk", region)
        {
            _metaNode = base.MetaNode as ElasticBeanstalkRootViewMetaNode;
            _beanstalkClient = new Lazy<IAmazonElasticBeanstalk>(CreateBeanstalkClient);
        }

        public override string ToolTip =>
            "AWS Elastic Beanstalk is an even easier way for you to quickly deploy " +
            "and manage applications in the AWS cloud. You can right click web applications " +
            "and web site projects and publish to beanstalk. " +
            "Elastic Beanstalk will automatically handle the deployment details of capacity " +
            "provisioning, load balancing, auto-scaling, and application health monitoring.";

        protected override string IconName => "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.rootviewnode.png";

        public IAmazonElasticBeanstalk BeanstalkClient => this._beanstalkClient.Value;


        protected override void LoadChildren()
        {
            var environmentsByApps = new Dictionary<string, List<EnvironmentDescription>>();
            var envRequest = new DescribeEnvironmentsRequest() { IncludeDeleted = false };
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)envRequest).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var envResponse = this.BeanstalkClient.DescribeEnvironments(envRequest);
            foreach (var environment in envResponse.Environments)
            {
                List<EnvironmentDescription> environments;
                if (!environmentsByApps.TryGetValue(environment.ApplicationName, out environments))
                {
                    environments = new List<EnvironmentDescription>();
                    environmentsByApps[environment.ApplicationName] = environments;
                }

                environments.Add(environment);
            }


            List<IViewModel> items = new List<IViewModel>();
            var appRequest = new DescribeApplicationsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)appRequest).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            var appResponse = this.BeanstalkClient.DescribeApplications(appRequest);
            foreach (var application in appResponse.Applications)
            {
                List<EnvironmentDescription> environments;
                if (!environmentsByApps.TryGetValue(application.ApplicationName, out environments))
                    environments = new List<EnvironmentDescription>();

                items.Add(new ApplicationViewModel(this._metaNode.ApplicationViewMetaNode, this, application, environments));
            }

            SetChildren(items);

        }

        public void RemoveApplication(string applicationName)
        {
            base.RemoveChild(applicationName);
        }

        private IAmazonElasticBeanstalk CreateBeanstalkClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonElasticBeanstalkClient>(Region);
        }
    }
}
