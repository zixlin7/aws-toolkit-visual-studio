using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;


using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


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

        public override string Name
        {
            get
            {
                return this._user.UserName;
            }
        }

        public User User
        {
            get { return this._user; }
        }

        public IAMUserRootViewModel IAMUserRootViewModel
        {
            get { return this._serviceModel; }
        }

        public IAmazonIdentityManagementService IAMClient
        {
            get { return this._serviceModel.IAMClient; }
        }

        public void UpdateUser(string userName)
        {
            this.User.UserName = userName;
            base.NotifyPropertyChanged("Name");
        }


        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.user-service-root.png";
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("PRINCIPAL.ARN", this._user.Arn);
        }
    }
}
