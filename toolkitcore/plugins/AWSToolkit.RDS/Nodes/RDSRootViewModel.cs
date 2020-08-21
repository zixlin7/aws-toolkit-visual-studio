using System;
using System.Collections.Generic;
using Amazon.RDS;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSRootViewModel : ServiceRootViewModel, IRDSRootViewModel
    {
        RDSRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;


        IAmazonRDS _rdsClient;

        public RDSRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<RDSRootViewMetaNode>(), accountViewModel, "Amazon RDS")
        {
            this._metaNode = base.MetaNode as RDSRootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip => "Amazon Relational Database Service (Amazon RDS) makes it easy to set up, operate, and scale a relational database in the cloud.";

        protected override string IconName => "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.ServiceIcon.png";

        public IAmazonRDS RDSClient => this._rdsClient;

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonRDSConfig ();
            this.CurrentEndPoint.ApplyToClientConfig(config);
            this._rdsClient = new AmazonRDSClient(awsCredentials, config);
        }


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
    }
}
