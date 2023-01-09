using System.Collections.ObjectModel;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewElasticIPsModel : BaseModel
    {
        private ObservableCollection<AddressWrapper> _addresses = new ObservableCollection<AddressWrapper>();
        public ObservableCollection<AddressWrapper> Addresses => _addresses;

        private EC2ColumnDefinition[] _propertyColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (_propertyColumnDefinitions == null)
                {
                    _propertyColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(AddressWrapper));
                }

                return _propertyColumnDefinitions;
            }
        }
    }
}
