using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            get { return this._actionSelectedAddress; }
            set
            {
                this._actionSelectedAddress = value;
                base.NotifyPropertyChanged("ActionSelectedAddress");
            }
        }

        public bool ActionCreateNewAddress
        {
            get { return !this._actionSelectedAddress; }
            set
            {
                this._actionSelectedAddress = !value;
                base.NotifyPropertyChanged("ActionCreateNewAddress");
            }
        }

        AddressWrapper _selectedAdress;
        public AddressWrapper SelectedAddress
        {
            get
            {
                return this._selectedAdress;
            }
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
