using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMGroupRootViewModel : InstanceDataRootViewModel, IIAMGroupRootViewModel
    {
        IAMGroupRootViewMetaNode _metaNode;
        IAMRootViewModel _serviceModel;

        public IAMGroupRootViewModel(IAMGroupRootViewMetaNode metaNode, IAMRootViewModel viewModel)
            : base(metaNode, viewModel, "Groups")
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
        }

        public IAmazonIdentityManagementService IAMClient
        {
            get { return this._serviceModel.IAMClient; }
        }

        public void AddGroup(Group group)
        {
            var node = new IAMGroupViewModel(this._metaNode.IAMGroupViewMetaNode, this, group);
            base.AddChild(node);
        }

        public void RemoveGroup(string groupName)
        {
            base.RemoveChild(groupName);
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.group-service-root.png";
            }
        }

        protected override void LoadChildren()
        {
            var request = new ListGroupsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.IAMClient.ListGroups(request);
            var items = response.Groups.Select(@group => new IAMGroupViewModel(this._metaNode.IAMGroupViewMetaNode, this, @group)).Cast<IViewModel>().ToList();

            BeginCopingChildren(items);
        }    
    }
}
