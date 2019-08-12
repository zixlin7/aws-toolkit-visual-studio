using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.EC2.Model
{
    public class ChangeShutdownBehaviorModel : BaseModel
    {
        public ChangeShutdownBehaviorModel(string instanceId)
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

        ObservableCollection<string> _options = new ObservableCollection<string>();
        public ObservableCollection<string> Options
        {
            get 
            {
                if (this._options.Count == 0)
                {
                    this._options.Add("stop");
                    this._options.Add("terminate");
                }
                return this._options; 
            }
        }

        string _selectedOption;
        public string SelectedOption
        {
            get => this._selectedOption;
            set
            {
                this._selectedOption = value;
                base.NotifyPropertyChanged("SelectedOption");
            }
        }

        string _initialOption;
        public string InitialOption
        {
            get => this._initialOption;
            set
            {
                this._initialOption = value;
                base.NotifyPropertyChanged("InitialOption");
            }
        }
    }
}
