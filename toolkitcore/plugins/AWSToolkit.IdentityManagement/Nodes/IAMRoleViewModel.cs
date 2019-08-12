using System.Windows;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMRoleViewModel : AbstractViewModel
    {
        IAMRoleViewMetaNode _metaNode;
        IAMRoleRootViewModel _serviceModel;
        Role _role;

        public IAMRoleViewModel(IAMRoleViewMetaNode metaNode, IAMRoleRootViewModel viewModel, Role role)
            : base(metaNode, viewModel, role.RoleName)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._role = role;
        }

        public override string Name => this._role.RoleName;

        public Role Role => this._role;

        public IAMRoleRootViewModel IAMRoleRootViewModel => this._serviceModel;

        public IAmazonIdentityManagementService IAMClient => this._serviceModel.IAMClient;

        public void UpdateRole(string roleName)
        {
            this.Role.RoleName = roleName;
            base.NotifyPropertyChanged("Name");
        }


        protected override string IconName => "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.role.png";

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
//            dndDataObjects.SetData("PRINCIPAL.ARN", this._role.Arn);
        }
    }
}
