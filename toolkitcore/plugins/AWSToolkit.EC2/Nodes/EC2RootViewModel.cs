using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2RootViewModel : EC2ServiceViewModel, IEC2RootViewModel
    {
        EC2RootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;

        public EC2RootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild <EC2RootViewMetaNode>(), accountViewModel, "Amazon EC2", region)
        {
            this._metaNode = base.MetaNode as EC2RootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip => "Amazon Elastic Compute Cloud delivers scalable, pay-as-you-go compute capacity in the cloud.";

        protected override string IconName => AwsImageResourcePath.Ec2.Path;


        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>();
                items.Add(new EC2AMIsViewModel(this.MetaNode.FindChild<EC2AMIsViewMetaNode>(), this));
                items.Add(new EC2InstancesViewModel(this.MetaNode.FindChild<EC2InstancesViewMetaNode>(), this));
                items.Add(new EC2KeyPairsViewModel(this.MetaNode.FindChild<EC2KeyPairsViewMetaNode>(), this));
                items.Add(new SecurityGroupsViewModel(this.MetaNode.FindChild<SecurityGroupsViewMetaNode>(), this));
                items.Add(new EC2VolumesViewModel(this.MetaNode.FindChild<EC2VolumesViewMetaNode>(), this));
                items.Add(new ElasticIPsViewModel(this.MetaNode.FindChild<ElasticIPsViewMetaNode>(), this));
                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }
    }
}
