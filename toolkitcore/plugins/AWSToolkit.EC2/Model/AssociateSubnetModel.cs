using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            get
            {
                return this._selectedSubnet;
            }
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
