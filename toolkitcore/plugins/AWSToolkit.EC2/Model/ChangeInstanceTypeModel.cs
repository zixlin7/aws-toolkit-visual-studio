using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ChangeInstanceTypeModel : BaseModel
    {
        public ChangeInstanceTypeModel(string instanceId)
        {
            this._instanceId = instanceId;
        }

        string _instanceId;
        public string InstanceId
        {
            get { return this._instanceId; }
            set
            {
                this._instanceId = value;
                base.NotifyPropertyChanged("InstanceId");
            }
        }

        ObservableCollection<InstanceType> _instanceTypes = new ObservableCollection<InstanceType>();
        public ObservableCollection<InstanceType> InstanceTypes
        {
            get { return this._instanceTypes; }
        }

        InstanceType _selectedInstanceType;
        public InstanceType SelectedInstanceType
        {
            get { return this._selectedInstanceType; }
            set
            {
                this._selectedInstanceType = value;
                base.NotifyPropertyChanged("SelectedInstanceType");
            }
        }
    }
}
