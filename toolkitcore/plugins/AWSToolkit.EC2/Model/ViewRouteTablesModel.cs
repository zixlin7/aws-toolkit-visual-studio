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
    public class ViewRouteTablesModel : BaseModel
    {
        ObservableCollection<RouteTableWrapper> _routeTables = new ObservableCollection<RouteTableWrapper>();
        public ObservableCollection<RouteTableWrapper> RouteTables
        {
            get { return this._routeTables; }
        }

        EC2ColumnDefinition[] _propertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._propertytColumnDefinitions == null)
                {
                    this._propertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(RouteTableWrapper));
                }

                return this._propertytColumnDefinitions;
            }
        }

        public string[] ListAvailableTags
        {
            get
            {
                return EC2ColumnDefinition.GetListAvailableTags(this.RouteTables);
            }
        }
    }
}
