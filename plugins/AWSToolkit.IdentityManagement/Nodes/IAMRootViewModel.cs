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
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRootViewModel : ServiceRootViewModel, IIAMRootViewModel
    {
        IAMRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        
        IAMGroupRootViewModel _iamGroupRootViewModel;
        IAMRoleRootViewModel _iamRoleRootViewModel;
        IAMUserRootViewModel _iamUserRootViewModel;
        

        IAmazonIdentityManagementService _iamClient;

        public IAMRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<IAMRootViewMetaNode>(), accountViewModel, "AWS Identity and Access Management")
        {
            this._metaNode = base.MetaNode as IAMRootViewMetaNode;
            this._accountViewModel = accountViewModel;            
        }

        public IAMGroupRootViewModel IAMGroupRootViewModel
        {
            get { return this._iamGroupRootViewModel; }
        }

        public IAMUserRootViewModel IAMUserRootViewModel
        {
            get { return this._iamUserRootViewModel; }
        }

        public IAMRoleRootViewModel IAMRoleRootViewModel
        {
            get { return this._iamRoleRootViewModel; }
        }

        public override string ToolTip
        {
            get
            {
                return "AWS Identity and Access Management (IAM) enables you to create multiple Users and manage the permissions for each of these Users within your AWS Account. A User is an identity (within your AWS Account) with unique security credentials that can be used to access AWS Services. IAM eliminates the need to share passwords or access keys, and makes it easy to enable or disable a User’s access as appropriate. IAM offers you greater flexibility, control and security when using AWS.";
            }
        }

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

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.service-root.png";
            }
        }

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonIdentityManagementServiceConfig {ServiceURL = this.CurrentEndPoint.Url};
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            this._iamClient = new AmazonIdentityManagementServiceClient(awsCredentials, config);
        }

        public IAmazonIdentityManagementService IAMClient
        {
            get { return this._iamClient; }
        }

        protected override void LoadChildren()
        {
            this._iamGroupRootViewModel = new IAMGroupRootViewModel(this._metaNode.IAMGroupRootViewMetaNode, this);
            this._iamRoleRootViewModel = new IAMRoleRootViewModel(this._metaNode.IAMRoleRootViewMetaNode, this);
            this._iamUserRootViewModel = new IAMUserRootViewModel(this._metaNode.IAMUserRootViewMetaNode, this);
            List<IViewModel> children = new List<IViewModel>();

            children.Add(this._iamGroupRootViewModel);
            children.Add(this._iamRoleRootViewModel);
            children.Add(this._iamUserRootViewModel);
            BeginCopingChildren(children);
        }
    }
}
