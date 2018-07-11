using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon;
using Amazon.RDS;
using Amazon.RDS.Model;

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

        public override string ToolTip
        {
            get
            {
                return "Amazon Relational Database Service (Amazon RDS) makes it easy to set up, operate, and scale a relational database in the cloud.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.ServiceIcon.png";
            }
        }

        public IAmazonRDS RDSClient
        {
            get { return this._rdsClient; }
        }

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

                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        public override bool FailedToLoadChildren
        {
            get
            {
                return this.Children[0].FailedToLoadChildren;
            }
        }
    }
}
