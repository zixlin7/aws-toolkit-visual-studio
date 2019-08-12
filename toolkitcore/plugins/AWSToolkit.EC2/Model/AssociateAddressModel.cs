using System.Collections.Generic;
using System.Linq;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AssociateAddressModel : BaseModel
    {

        public AssociateAddressModel(AddressWrapper address)
        {
            this._address = address;
        }

        AddressWrapper _address;
        public AddressWrapper Address
        {
            get => this._address;
            set
            {
                this._address = value;
                base.NotifyPropertyChanged("Address");
            }
        }

        InstanceItem _instanceId;
        public InstanceItem Instance
        {
            get => this._instanceId;
            set
            {
                this._instanceId = value;
                base.NotifyPropertyChanged("Instance");
            }
        }

        public List<InstanceItem> AvailableInstances
        {
            get;
            set;
        }


        public class InstanceItem
        {
            public InstanceItem(Instance instance)
            {
                this.InstanceId = instance.InstanceId;
                this.DisplayName = instance.InstanceId;

                var tag = instance.Tags.FirstOrDefault(x => x.Key == EC2Constants.TAG_NAME);
                if(tag != null)
                    this.DisplayName += " - " + tag.Value;
            }

            public string InstanceId
            {
                get;
            }

            public string DisplayName
            {
                get;
            }
        }
    }
}
