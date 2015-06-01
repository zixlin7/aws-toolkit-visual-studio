using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ElasticBeanstalkRootViewModel : ServiceRootViewModel, IElasticBeanstalkRootViewModel
    {
        ElasticBeanstalkRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;

        IAmazonElasticBeanstalk _beanstalkClient;

        public ElasticBeanstalkRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<ElasticBeanstalkRootViewMetaNode>(), accountViewModel, "AWS Elastic Beanstalk")
        {
            this._metaNode = base.MetaNode as ElasticBeanstalkRootViewMetaNode;
            this._accountViewModel = accountViewModel;            
        }

        public override string ToolTip
        {
            get
            {
                return "AWS Elastic Beanstalk is an even easier way for you to quickly deploy " +
                       "and manage applications in the AWS cloud. You can right click web applications " +
                       "and web site projects and publish to beanstalk. " +
                       "Elastic Beanstalk will automatically handle the deployment details of capacity " +
                       "provisioning, load balancing, auto-scaling, and application health monitoring.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.rootviewnode.png";
            }
        }

        protected override void BuildClient(string accessKey, string secretKey)
        {
            var config = new AmazonElasticBeanstalkConfig {ServiceURL = this.CurrentEndPoint.Url};
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._beanstalkClient = new AmazonElasticBeanstalkClient(accessKey, secretKey, config);
        }

        public IAmazonElasticBeanstalk BeanstalkClient
        {
            get {return this._beanstalkClient;}
        }


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

            BeginCopingChildren(items);

        }

        public void RemoveApplication(string applicationName)
        {
            base.RemoveChild(applicationName);
        }
    }
}
