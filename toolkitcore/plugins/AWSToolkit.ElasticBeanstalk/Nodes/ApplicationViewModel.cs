using System.Collections.Generic;
using System.Windows;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ApplicationViewModel : InstanceDataRootViewModel, IApplicationViewModel
    {
        ApplicationViewMetaNode _metaNode;
        ElasticBeanstalkRootViewModel _serviceModel;
        ApplicationDescription _application;

        public ApplicationViewModel(ApplicationViewMetaNode metaNode, ElasticBeanstalkRootViewModel viewModel, ApplicationDescription application)
            : base(metaNode, viewModel, application.ApplicationName)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._application = application;

            this.Region = viewModel.Region;
        }

        public ApplicationViewModel(ApplicationViewMetaNode metaNode, ElasticBeanstalkRootViewModel viewModel, ApplicationDescription application, IEnumerable<EnvironmentDescription> environments)
            : this(metaNode, viewModel, application)
        {
            AddEnvironments(environments);
        }

        public ToolkitRegion Region
        {
            get;
        }

        public ElasticBeanstalkRootViewModel ElasticBeanstalkRootViewModel => this._serviceModel;

        public IAmazonElasticBeanstalk BeanstalkClient => this._serviceModel.BeanstalkClient;

        public ApplicationDescription Application => this._application;

        protected override string IconName => "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.application.png";

        protected override void LoadChildren()
        {
            var request = new DescribeEnvironmentsRequest() { ApplicationName = this.Application.ApplicationName, IncludeDeleted = false };
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.BeanstalkClient.DescribeEnvironments(request);

            AddEnvironments(response.Environments);
        }

        private void AddEnvironments(IEnumerable<EnvironmentDescription> environments)
        {
            List<IViewModel> items = new List<IViewModel>();
            foreach (var Environment in environments)
            {
                items.Add(new EnvironmentViewModel(this._metaNode.EnvironmentViewMetaNode, this, Environment));
            }

            SetChildren(items);
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:elasticbeanstalk:{0}:{1}:application/{2}",
                this.ElasticBeanstalkRootViewModel.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId, this.Name));
        }
    }
}
