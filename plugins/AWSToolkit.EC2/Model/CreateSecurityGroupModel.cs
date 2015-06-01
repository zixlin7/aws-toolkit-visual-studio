using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateSecurityGroupModel : BaseModel
    {
        string _name;
        public string Name
        {
            get { return this._name; }
            set
            {
                this._name = value;
                base.NotifyPropertyChanged("Name");
            }
        }

        string _description;
        public string Description
        {
            get { return this._description; }
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
