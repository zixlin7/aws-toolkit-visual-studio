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
    public class IAMGroupViewModel : AbstractViewModel, IIAMGroupViewModel
    {
        IAMGroupViewMetaNode _metaNode;
        IAMGroupRootViewModel _serviceModel;
        Group _group;

        public IAMGroupViewModel(IAMGroupViewMetaNode metaNode, IAMGroupRootViewModel viewModel, Group group)
            : base(metaNode, viewModel, group.GroupName)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._group = group;
        }

        public IAMGroupRootViewModel IAMGroupRootViewModel
        {
            get { return this._serviceModel; }
        }

        public IAmazonIdentityManagementService IAMClient
        {
            get { return this._serviceModel.IAMClient; }
        }

        public Group Group
        {
            get { return this._group; }
        }

        public void UpdateGroup(string groupName)
        {
            this.Group.GroupName = groupName;
            base.NotifyPropertyChanged("Name");
        }


        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.group-service-root.png";
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
        }
    }
}
