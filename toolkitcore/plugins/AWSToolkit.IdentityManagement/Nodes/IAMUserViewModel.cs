using System.Windows;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMUserViewModel : AbstractViewModel, IIAMUserViewModel
    {
        IAMUserViewMetaNode _metaNode;
        IAMUserRootViewModel _serviceModel;
        User _user;

        public IAMUserViewModel(IAMUserViewMetaNode metaNode, IAMUserRootViewModel viewModel, User user)
            : base(metaNode, viewModel, user.UserName)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._user = user;
        }

        public override string Name => this._user.UserName;

        public User User => this._user;

        public IAMUserRootViewModel IAMUserRootViewModel => this._serviceModel;

        public IAmazonIdentityManagementService IAMClient => this._serviceModel.IAMClient;

        public void UpdateUser(string userName)
        {
            this.User.UserName = userName;
            base.NotifyPropertyChanged("Name");
        }


        protected override string IconName => AwsImageResourcePath.IamUser.Path;

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("PRINCIPAL.ARN", this._user.Arn);
        }
    }
}
