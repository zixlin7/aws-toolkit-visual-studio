using Amazon.RDS;

//using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecurityGroupViewModel : AbstractViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(RDSInstanceViewModel));

        IAmazonRDS _rdsClient;
        DBSecurityGroupWrapper _group;

        public RDSSecurityGroupViewModel(RDSSecurityGroupViewMetaNode metaNode, RDSSecurityGroupRootViewModel viewModel, DBSecurityGroupWrapper group)
            : base(metaNode, viewModel, group.DisplayName)
        {
            this._group = group;
            this._rdsClient = viewModel.RDSClient;
        }

        protected override string IconName => "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.SecurityGroup.png";

        public IAmazonRDS RDSClient => this._rdsClient;

        public DBSecurityGroupWrapper DBGroup => this._group;
    }
}
