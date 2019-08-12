using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class ViewDBSubnetGroupsModel : BaseModel
    {
        readonly ObservableCollection<DBSubnetGroupWrapper> _subnetGroups = new ObservableCollection<DBSubnetGroupWrapper>();

        public ObservableCollection<DBSubnetGroupWrapper> DBSubnetGroups => this._subnetGroups;

        readonly IList<DBSubnetGroupWrapper> _selectedDBSubnetGroups = new List<DBSubnetGroupWrapper>();

        public IList<DBSubnetGroupWrapper> SelectedDBSubnetGroups => _selectedDBSubnetGroups;

        EC2ColumnDefinition[] _subnetGroupPropertyColumnDefinitions;
        
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._subnetGroupPropertyColumnDefinitions == null)
                {
                    this._subnetGroupPropertyColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(DBSubnetGroupWrapper));
                }

                return this._subnetGroupPropertyColumnDefinitions;
            }
        }


    }
}
