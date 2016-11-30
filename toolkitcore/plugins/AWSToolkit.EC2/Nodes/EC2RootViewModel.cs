using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2RootViewModel : EC2ServiceViewModel, IEC2RootViewModel
    {
        EC2RootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;

        public EC2RootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild <EC2RootViewMetaNode>(), accountViewModel, "Amazon EC2")
        {
            this._metaNode = base.MetaNode as EC2RootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip
        {
            get
            {
                return "Amazon Elastic Compute Cloud delivers scalable, pay-as-you-go compute capacity in the cloud.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.service-root-icon.png";
            }
        }


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
                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }
    }
}
