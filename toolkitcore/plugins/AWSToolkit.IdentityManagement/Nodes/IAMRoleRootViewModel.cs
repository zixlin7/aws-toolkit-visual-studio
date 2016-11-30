using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRoleRootViewModel : InstanceDataRootViewModel
    {
        IAMRoleRootViewMetaNode _metaNode;
        IAMRootViewModel _serviceModel;
         
        public IAMRoleRootViewModel(IAMRoleRootViewMetaNode metaNode, IAMRootViewModel viewModel)
            : base(metaNode, viewModel, "Roles")
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
        }

        public IAMRootViewModel IAMRootViewModel
        {
            get { return this._serviceModel; }
        }

        public IAmazonIdentityManagementService IAMClient
        {
            get { return this._serviceModel.IAMClient; }
        }

        public void AddRole(Role role)
        {
            var node = new IAMRoleViewModel(this._metaNode.IAMRoleViewMetaNode, this, role);
            base.AddChild(node);
        }

        public void RemoveRole(string rolename)
        {
            base.RemoveChild(rolename);
        }



        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.role.png";
            }
        }

        protected override void LoadChildren()
        {
            var request = new ListRolesRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            var response = this.IAMClient.ListRoles(request);
            var items = response.Roles.Select(role => new IAMRoleViewModel(this._metaNode.IAMRoleViewMetaNode, this, role)).Cast<IViewModel>().ToList();

            BeginCopingChildren(items);
        }    
    }
}
