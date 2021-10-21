using Amazon.RDS;

//using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSInstanceViewModel : AbstractViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(RDSInstanceViewModel));

        readonly RDSInstanceRootViewModel _parentViewModel;
        readonly IAmazonRDS _rdsClient;
        readonly DBInstanceWrapper _instance;

        public RDSInstanceViewModel(RDSInstanceViewMetaNode metaNode, RDSInstanceRootViewModel viewModel, DBInstanceWrapper instance)
            : base(metaNode, viewModel, instance.DBInstanceIdentifier)
        {
            this._parentViewModel = viewModel;
            this._instance = instance;
            this._rdsClient = viewModel.RDSClient;
        }

        public IAmazonRDS RDSClient => this._rdsClient;

        public RDSInstanceRootViewModel InstanceRootViewModel => this._parentViewModel;

        protected override string IconName => AwsImageResourcePath.RdsDbInstances.Path;

        public DBInstanceWrapper DBInstance => this._instance;
    }
}
