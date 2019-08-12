using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.EC2.Model
{
    public class ChooseInstanceToConnectModel : BaseModel
    {

        IList<string> _instanceIds;
        public IList<string> InstanceIds
        {
            get => this._instanceIds;
            set
            {
                this._instanceIds = value;
                base.NotifyPropertyChanged("InstanceIds");
            }
        }

        string _selectedInstance;
        public string SelectedInstance
        {
            get => this._selectedInstance;
            set
            {
                this._selectedInstance = value;
                base.NotifyPropertyChanged("SelectedInstance");
            }
        }
    }
}
