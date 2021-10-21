using System.Windows;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class EnvironmentViewModel : AbstractViewModel, IEnvironmentViewModel
    {
        EnvironmentViewMetaNode _metaNode;
        ApplicationViewModel _applicationModel;
        EnvironmentDescription _Environment;

        public EnvironmentViewModel(EnvironmentViewMetaNode metaNode, ApplicationViewModel applicationModel, EnvironmentDescription Environment)
            : base(metaNode, applicationModel, Environment.EnvironmentName)
        {
            this._metaNode = metaNode;
            this._applicationModel = applicationModel;
            this._Environment = Environment;
        }

        public ApplicationViewModel ApplicationViewModel => this._applicationModel;

        public IAmazonElasticBeanstalk BeanstalkClient => this._applicationModel.BeanstalkClient;

        public EnvironmentDescription Environment => this._Environment;

        protected override string IconName => AwsImageResourcePath.ElasticBeanstalkEnvironment.Path;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {            
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:elasticbeanstalk:{0}:{1}:environment/{2}/{3}",
                this.ApplicationViewModel.ElasticBeanstalkRootViewModel.Region.Id, ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId, this.ApplicationViewModel.Name, this.Name));
        }
    }
}
