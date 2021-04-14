using System;
using System.Collections.Generic;
using Amazon.RDS;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSRootViewModel : ServiceRootViewModel, IRDSRootViewModel
    {
        private readonly RDSRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonRDS> _rdsClient;

        public RDSRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<RDSRootViewMetaNode>(), accountViewModel, "Amazon RDS", region)
        {
            _metaNode = base.MetaNode as RDSRootViewMetaNode;
            _rdsClient = new Lazy<IAmazonRDS>(CreateRdsClient);
        }

        public override string ToolTip => "Amazon Relational Database Service (Amazon RDS) makes it easy to set up, operate, and scale a relational database in the cloud.";

        protected override string IconName => "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.ServiceIcon.png";

        public IAmazonRDS RDSClient => this._rdsClient.Value;

        protected override void LoadChildren()
        {
            try
            {
                var items = new List<IViewModel>
                {
                    new RDSInstanceRootViewModel(this.MetaNode.FindChild<RDSInstanceRootViewMetaNode>(), this), 
                    new RDSSubnetGroupsRootViewModel(this.MetaNode.FindChild<RDSSubnetGroupsRootViewMetaNode>(), this), 
                    new RDSSecurityGroupRootViewModel(this.MetaNode.FindChild<RDSSecurityGroupRootViewMetaNode>(), this)
                };

                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        public override bool FailedToLoadChildren => this.Children[0].FailedToLoadChildren;

        private IAmazonRDS CreateRdsClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonRDSClient>(Region);
        }
    }
}
