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

        public IAmazonRDS RDSClient
        {
            get { return this._rdsClient; }
        }

        public RDSInstanceRootViewModel InstanceRootViewModel
        {
            get { return this._parentViewModel; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBInstances.png";
            }
        }

        public DBInstanceWrapper DBInstance
        {
            get { return this._instance; }
        }

    }
}
