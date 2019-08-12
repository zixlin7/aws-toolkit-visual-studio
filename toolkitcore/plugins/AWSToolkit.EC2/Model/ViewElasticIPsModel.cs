using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewElasticIPsModel : BaseModel
    {
        ObservableCollection<AddressWrapper> _addresses = new ObservableCollection<AddressWrapper>();
        public ObservableCollection<AddressWrapper> Addresses => this._addresses;

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
