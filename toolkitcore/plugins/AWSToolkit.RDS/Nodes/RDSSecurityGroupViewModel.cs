using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;

using Amazon.RDS;
using Amazon.RDS.Model;

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

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.SecurityGroup.png";
            }
        }

        public IAmazonRDS RDSClient
        {
            get { return this._rdsClient; }
        }

        public DBSecurityGroupWrapper DBGroup
        {
            get { return this._group; }
        }
    }
}
