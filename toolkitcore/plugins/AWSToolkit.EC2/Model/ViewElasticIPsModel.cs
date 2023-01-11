using System.Collections.ObjectModel;
using System.Windows.Input;

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

        private ICommand _allocateElasticIp;

        public ICommand AllocateElasticIp
        {
            get => _allocateElasticIp;
            set => SetProperty(ref _allocateElasticIp, value);
        }

        private ICommand _releaseElasticIp;

        public ICommand ReleaseElasticIp
        {
            get => _releaseElasticIp;
            set => SetProperty(ref _releaseElasticIp, value);
        }

        private ICommand _associateElasticIp;

        public ICommand AssociateElasticIp
        {
            get => _associateElasticIp;
            set => SetProperty(ref _associateElasticIp, value);
        }

        private ICommand _disassociateElasticIp;

        public ICommand DisassociateElasticIp
        {
            get => _disassociateElasticIp;
            set => SetProperty(ref _disassociateElasticIp, value);
        }
    }
}
