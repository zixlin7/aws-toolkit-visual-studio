using Amazon.RDS;
using Amazon.RDS.Model;

//using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSubnetGroupViewModel : AbstractViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(RDSSubnetGroupViewModel));

        readonly IAmazonRDS _rdsClient;
        readonly DBSubnetGroupWrapper _subnetGroup;

        public RDSSubnetGroupViewModel(RDSSubnetGroupViewMetaNode metaNode, RDSSubnetGroupsRootViewModel viewModel, DBSubnetGroup subnetGroup)
            : base(metaNode, viewModel, subnetGroup.DBSubnetGroupName)
        {
            this._subnetGroup = new DBSubnetGroupWrapper(subnetGroup);
            this._rdsClient = viewModel.RDSClient;
        }

        public IAmazonRDS RDSClient => this._rdsClient;

        protected override string IconName => AwsImageResourcePath.RdsSubnetGroups.Path;

        public DBSubnetGroupWrapper SubnetGroup => this._subnetGroup;
    }
}
