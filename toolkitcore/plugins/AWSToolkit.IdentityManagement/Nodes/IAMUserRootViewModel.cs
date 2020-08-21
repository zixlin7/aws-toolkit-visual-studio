using System.Linq;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMUserRootViewModel : InstanceDataRootViewModel, IIAMUserRootViewModel
    {
        IAMUserRootViewMetaNode _metaNode;
        IAMRootViewModel _serviceModel;
         
        public IAMUserRootViewModel(IAMUserRootViewMetaNode metaNode, IAMRootViewModel viewModel)
            : base(metaNode, viewModel, "Users")
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
        }

        public IAMRootViewModel IAMRootViewModel => this._serviceModel;

        public IAmazonIdentityManagementService IAMClient => this._serviceModel.IAMClient;

        public void AddUser(User user)
        {
            var node = new IAMUserViewModel(this._metaNode.IAMUserViewMetaNode, this, user);
            base.AddChild(node);
        }

        public void RemoveUser(string username)
        {
            base.RemoveChild(username);
        }



        protected override string IconName => "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.user-service-root.png";

        protected override void LoadChildren()
        {
            var request = new ListUsersRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.IAMClient.ListUsers(request);
            var items = response.Users.Select(user => new IAMUserViewModel(this._metaNode.IAMUserViewMetaNode, this, user)).Cast<IViewModel>().ToList();

            SetChildren(items);
        }
    }
}
