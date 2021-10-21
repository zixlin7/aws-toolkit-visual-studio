﻿using System.Linq;
using Amazon.RDS;
using Amazon.RDS.Model;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using log4net;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecurityGroupRootViewModel : InstanceDataRootViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(RDSSecurityGroupRootViewModel));

        RDSRootViewModel _rootViewModel;
        IAmazonRDS _rdsClient;

        public RDSSecurityGroupRootViewModel(RDSSecurityGroupRootViewMetaNode metaNode, RDSRootViewModel viewModel)
            : base(metaNode, viewModel, "Security Groups")
        {
            this._rootViewModel = viewModel;
            this._rdsClient = viewModel.RDSClient;
        }

        public IAmazonRDS RDSClient => this._rdsClient;

        protected override string IconName => AwsImageResourcePath.RdsSecurityGroup.Path;

        protected override void LoadChildren()
        {
            var request = new DescribeDBSecurityGroupsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.RDSClient.DescribeDBSecurityGroups(request);
            var items = response.DBSecurityGroups.Select(@group => new RDSSecurityGroupViewModel((RDSSecurityGroupViewMetaNode) this.MetaNode.FindChild<RDSSecurityGroupViewMetaNode>(), this, new DBSecurityGroupWrapper(@group))).Cast<IViewModel>().ToList();

            SetChildren(items);
        }

        public ToolkitRegion Region => _rootViewModel.Region;

        public void RemoveSecurityGroup(string securityGroupName)
        {
            base.RemoveChild(securityGroupName);
        }

        public void AddSecurityGroup(DBSecurityGroupWrapper securityGroup)
        {
            var child = new RDSSecurityGroupViewModel((RDSSecurityGroupViewMetaNode)this.MetaNode.FindChild<RDSSecurityGroupViewMetaNode>(), this, securityGroup);
            base.AddChild(child);
        }
    }
}
