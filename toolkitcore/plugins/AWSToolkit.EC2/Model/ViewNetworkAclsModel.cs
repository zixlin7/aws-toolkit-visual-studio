using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Utils;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewNetworkAclsModel : BaseModel
    {
        ObservableCollection<NetworkAclWrapper> _NetworkAcls = new ObservableCollection<NetworkAclWrapper>();
        public ObservableCollection<NetworkAclWrapper> NetworkAcls
        {
            get { return this._NetworkAcls; }
        }

        EC2ColumnDefinition[] _propertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._propertytColumnDefinitions == null)
                {
                    this._propertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(NetworkAclWrapper));
                }

                return this._propertytColumnDefinitions;
            }
        }

        public string[] ListAvailableTags
        {
            get
            {
                return EC2ColumnDefinition.GetListAvailableTags(this.NetworkAcls);
            }
        }
    }
}
