using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateSecurityGroupModel : BaseModel
    {
        string _name;
        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                base.NotifyPropertyChanged("Name");
            }
        }

        string _description;
        public string Description
        {
            get => this._description;
            set
            {
                this._description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        public VPCWrapper SelectedVPC { get; set; }

        public List<VPCWrapper> AvailableVPCs
        {
            get;
            set;
        }

        public bool IsVpcOnlyEnvironment { get; set; }
    }
}
