using System.Collections.ObjectModel;
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
            get => this._instanceId;
            set
            {
                this._instanceId = value;
                base.NotifyPropertyChanged("InstanceId");
            }
        }

        ObservableCollection<InstanceType> _instanceTypes = new ObservableCollection<InstanceType>();
        public ObservableCollection<InstanceType> InstanceTypes => this._instanceTypes;

        InstanceType _selectedInstanceType;
        public InstanceType SelectedInstanceType
        {
            get => this._selectedInstanceType;
            set
            {
                this._selectedInstanceType = value;
                base.NotifyPropertyChanged("SelectedInstanceType");
            }
        }
    }
}
