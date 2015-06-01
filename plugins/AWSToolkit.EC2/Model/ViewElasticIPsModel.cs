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
    public class ViewElasticIPsModel : BaseModel
    {
        ObservableCollection<AddressWrapper> _addresses = new ObservableCollection<AddressWrapper>();
        public ObservableCollection<AddressWrapper> Addresses
        {
            get { return this._addresses; }
        }

        EC2ColumnDefinition[] _propertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._propertytColumnDefinitions == null)
                {
                    this._propertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(AddressWrapper));
                }

                return this._propertytColumnDefinitions;
            }
        }
    }
}
