using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class VPCRootViewModel : EC2ServiceViewModel
    {
        EC2RootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;

        public VPCRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild <VPCRootViewMetaNode>(), accountViewModel, "Amazon VPC")
        {
            this._metaNode = base.MetaNode as EC2RootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip => "Amazon Virtual Private Cloud (Amazon VPC) lets you provision a private, isolated section of the Amazon Web Services (AWS) Cloud where you can launch AWS resources in a virtual network that you define.";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.vpc_service_root.png";

        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>();
                items.Add(new VPCsViewModel(this.MetaNode.FindChild<VPCsViewMetaNode>(), this));
                items.Add(new InternetGatewayViewModel(this.MetaNode.FindChild<InternetGatewayViewMetaNode>(), this));
                items.Add(new SubnetViewModel(this.MetaNode.FindChild<SubnetViewMetaNode>(), this));
                items.Add(new ElasticIPsViewModel(this.MetaNode.FindChild<ElasticIPsViewMetaNode>(), this));
                items.Add(new RouteTableViewModel(this.MetaNode.FindChild<RouteTableViewMetaNode>(), this));
                items.Add(new NetworkAclViewModel(this.MetaNode.FindChild<NetworkAclViewMetaNode>(), this));
                items.Add(new SecurityGroupsViewModel(this.MetaNode.FindChild<SecurityGroupsViewMetaNode>(), this));
                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }
    }
}
