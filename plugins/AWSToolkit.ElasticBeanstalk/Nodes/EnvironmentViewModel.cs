using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Amazon;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;


using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

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

        public ApplicationViewModel ApplicationViewModel
        {
            get { return this._applicationModel; }
        }

        public IAmazonElasticBeanstalk BeanstalkClient
        {
            get { return this._applicationModel.BeanstalkClient; }
        }

        public EnvironmentDescription Environment
        {
            get { return this._Environment; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.environment.png";
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {            
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", string.Format("arn:aws:elasticbeanstalk:{0}:{1}:environment/{2}/{3}",
                this.ApplicationViewModel.ElasticBeanstalkRootViewModel.CurrentEndPoint.RegionSystemName, this.AccountViewModel.AccountNumber, this.ApplicationViewModel.Name, this.Name));
        }
    }
}
