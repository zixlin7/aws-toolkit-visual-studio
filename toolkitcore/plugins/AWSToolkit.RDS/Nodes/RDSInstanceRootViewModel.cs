using System.Linq;
using Amazon.RDS;
using Amazon.RDS.Model;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using log4net;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSInstanceRootViewModel : InstanceDataRootViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(RDSInstanceRootViewModel));

        readonly RDSRootViewModel _rootViewModel;
        readonly IAmazonRDS _rdsClient;

        public RDSInstanceRootViewModel(RDSInstanceRootViewMetaNode metaNode, RDSRootViewModel viewModel)
            : base(metaNode, viewModel, "Instances")
        {
            this._rootViewModel = viewModel;
            this._rdsClient = viewModel.RDSClient;
        }

        public IAmazonRDS RDSClient => this._rdsClient;

        public RDSRootViewModel RDSRootViewModel => this._rootViewModel;

        protected override string IconName => AwsImageResourcePath.RdsDbInstances.Path;

        protected override void LoadChildren()
        {
            var request = new DescribeDBInstancesRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.RDSClient.DescribeDBInstances(request);
            var items = response.DBInstances.Select(instance => new RDSInstanceViewModel((RDSInstanceViewMetaNode) this.MetaNode.FindChild<RDSInstanceViewMetaNode>(), this, new DBInstanceWrapper(instance))).Cast<IViewModel>().ToList();

            SetChildren(items);
        }

        public ToolkitRegion Region => _rootViewModel.Region;

        public void RemoveDBInstance(string dbIdentifier)
        {
            base.RemoveChild(dbIdentifier);
        }

        public void AddDBInstance(DBInstanceWrapper instance)
        {
            var child = new RDSInstanceViewModel((RDSInstanceViewMetaNode)this.MetaNode.FindChild<RDSInstanceViewMetaNode>(), this, instance);
            base.AddChild(child);
        }
    }
}
