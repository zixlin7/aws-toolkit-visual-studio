using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSRootViewModel : ECSServiceRootViewModel, IECSRootViewModel
    {
        ECSRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;

        public ECSRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild <ECSRootViewMetaNode>(), accountViewModel, "Amazon ECS")
        {
            this._metaNode = base.MetaNode as ECSRootViewMetaNode;
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
                return "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.service-root-icon.png";
            }
        }


        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>
                {
                    new ECSClustersRootViewModel(this.MetaNode.FindChild<ECSClustersRootViewMetaNode>(), this),
                };
                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }
    }
}
