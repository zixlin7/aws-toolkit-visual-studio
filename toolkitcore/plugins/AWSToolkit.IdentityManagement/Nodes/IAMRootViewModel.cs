using System;
using System.Collections.Generic;
using Amazon.IdentityManagement;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRootViewModel : ServiceRootViewModel, IIAMRootViewModel
    {
        private readonly IAMRootViewMetaNode _metaNode;
        
        IAMGroupRootViewModel _iamGroupRootViewModel;
        IAMRoleRootViewModel _iamRoleRootViewModel;
        IAMUserRootViewModel _iamUserRootViewModel;
        
        private readonly Lazy<IAmazonIdentityManagementService> _iamClient;

        public IAMRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<IAMRootViewMetaNode>(), accountViewModel, "AWS Identity and Access Management", region)
        {
            _metaNode = base.MetaNode as IAMRootViewMetaNode;
            _iamClient = new Lazy<IAmazonIdentityManagementService>(CreateIamClient);
        }

        public IAMGroupRootViewModel IAMGroupRootViewModel => this._iamGroupRootViewModel;

        public IAMUserRootViewModel IAMUserRootViewModel => this._iamUserRootViewModel;

        public IAMRoleRootViewModel IAMRoleRootViewModel => this._iamRoleRootViewModel;

        public override string ToolTip => "AWS Identity and Access Management (IAM) enables you to create multiple Users and manage the permissions for each of these Users within your AWS Account. A User is an identity (within your AWS Account) with unique security credentials that can be used to access AWS Services. IAM eliminates the need to share passwords or access keys, and makes it easy to enable or disable a User’s access as appropriate. IAM offers you greater flexibility, control and security when using AWS.";

        public override void Refresh(bool async)
        {
            if (this.IAMUserRootViewModel == null)
            {
                base.Refresh(async);
            }
            else
            {
                this.IAMUserRootViewModel.Refresh(async);
                this.IAMGroupRootViewModel.Refresh(async);
            }
        }

        protected override string IconName => AwsImageResourcePath.IdentityAndAccessManagement.Path;

        public IAmazonIdentityManagementService IAMClient => this._iamClient.Value;

        protected override void LoadChildren()
        {
            this._iamGroupRootViewModel = new IAMGroupRootViewModel(this._metaNode.IAMGroupRootViewMetaNode, this);
            this._iamRoleRootViewModel = new IAMRoleRootViewModel(this._metaNode.IAMRoleRootViewMetaNode, this);
            this._iamUserRootViewModel = new IAMUserRootViewModel(this._metaNode.IAMUserRootViewMetaNode, this);
            List<IViewModel> children = new List<IViewModel>();

            children.Add(this._iamGroupRootViewModel);
            children.Add(this._iamRoleRootViewModel);
            children.Add(this._iamUserRootViewModel);
            SetChildren(children);
        }

        private IAmazonIdentityManagementService CreateIamClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonIdentityManagementServiceClient>(Region);
        }
    }
}
