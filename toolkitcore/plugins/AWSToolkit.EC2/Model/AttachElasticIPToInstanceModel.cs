using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AttachElasticIPToInstanceModel : BaseModel
    {
        public AttachElasticIPToInstanceModel(RunningInstanceWrapper instance)
        {
            this.Instance = instance;
        }

        public RunningInstanceWrapper Instance
        {
            get;
            set;
        }

        bool _actionSelectedAddress;
        public bool ActionSelectedAddress
        {
            get => this._actionSelectedAddress;
            set
            {
                this._actionSelectedAddress = value;
                base.NotifyPropertyChanged("ActionSelectedAddress");
            }
        }

        public bool ActionCreateNewAddress
        {
            get => !this._actionSelectedAddress;
            set
            {
                this._actionSelectedAddress = !value;
                base.NotifyPropertyChanged("ActionCreateNewAddress");
            }
        }

        AddressWrapper _selectedAdress;
        public AddressWrapper SelectedAddress
        {
            get => this._selectedAdress;
            set
            {
                this._selectedAdress = value;
                base.NotifyPropertyChanged("SelectedAddress");
            }
        }


        public List<AddressWrapper> AvailableAddresses
        {
            get;
            set;
        }
    }
}
