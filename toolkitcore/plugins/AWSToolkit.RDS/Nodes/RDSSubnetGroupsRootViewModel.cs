using System.Linq;
using Amazon.RDS;
using Amazon.RDS.Model;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using log4net;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSubnetGroupsRootViewModel : InstanceDataRootViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(RDSSubnetGroupsRootViewModel));

        readonly RDSRootViewModel _rootViewModel;
        readonly IAmazonRDS _rdsClient;

        public RDSSubnetGroupsRootViewModel(RDSSubnetGroupsRootViewMetaNode metaNode, RDSRootViewModel viewModel)
            : base(metaNode, viewModel, "Subnet Groups")
        {
            this._rootViewModel = viewModel;
            this._rdsClient = viewModel.RDSClient;
        }

        public IAmazonRDS RDSClient => this._rdsClient;

        protected override string IconName => AwsImageResourcePath.RdsSubnetGroups.Path;

        protected override void LoadChildren()
        {
            var request = new DescribeDBSubnetGroupsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.RDSClient.DescribeDBSubnetGroups();
            var items = response.DBSubnetGroups.Select(subnetGroup => new RDSSubnetGroupViewModel(this.MetaNode.FindChild<RDSSubnetGroupViewMetaNode>(), this, subnetGroup)).Cast<IViewModel>().ToList();

            SetChildren(items);
        }

        public ToolkitRegion Region => _rootViewModel.Region;

        public void RemoveDBSubnetGroup(string subnetGroupIdentifier)
        {
            base.RemoveChild(subnetGroupIdentifier);
        }

        public void AddDBSubnetGroup(DBSubnetGroup subnetGroup)
        {
            var child = new RDSSubnetGroupViewModel(this.MetaNode.FindChild<RDSSubnetGroupViewMetaNode>(), this, subnetGroup);
            base.AddChild(child);
        }
    }
}
