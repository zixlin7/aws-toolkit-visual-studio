using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AssociateSubnetModel : BaseModel
    {
        public AssociateSubnetModel()
        {
          
        }

        SubnetWrapper _selectedSubnet;
        public SubnetWrapper SelectedSubnet
        {
            get => this._selectedSubnet;
            set
            {
                this._selectedSubnet = value;
                base.NotifyPropertyChanged("SelectedSubnet");
            }
        }

        public IList<SubnetWrapper> AvailableSubnets
        {
            get;
            set;
        }
    }
}
