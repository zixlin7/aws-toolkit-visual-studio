using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewInstancesModel : BaseModel
    {
        ObservableCollection<RunningInstanceWrapper> _runningInstances = new ObservableCollection<RunningInstanceWrapper>();
        public ObservableCollection<RunningInstanceWrapper> RunningInstances => this._runningInstances;

        IList<RunningInstanceWrapper> _selectedInstances = new List<RunningInstanceWrapper>();
        public IList<RunningInstanceWrapper> SelectedInstances => this._selectedInstances;

        RunningInstanceWrapper _focusInstance;
        public RunningInstanceWrapper FocusInstance
        {
            get => this._focusInstance;
            set
            {
                this._focusInstance = value;
                base.NotifyPropertyChanged("FocusInstance");
            }
        }

        EC2ColumnDefinition[] _instancePropertytColumnDefinitions;
        public EC2ColumnDefinition[] InstancePropertyColumnDefinitions
        {
            get
            {
                if (this._instancePropertytColumnDefinitions == null)
                {
                    this._instancePropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(RunningInstanceWrapper));
                }

                return this._instancePropertytColumnDefinitions;
            }
        }


        public string[] ListInstanceAvailableTags => EC2ColumnDefinition.GetListAvailableTags(this.RunningInstances);

        EC2ColumnDefinition[] _volumePropertytColumnDefinitions;
        public EC2ColumnDefinition[] VolumePropertyColumnDefinitions
        {
            get
            {
                if (this._volumePropertytColumnDefinitions == null)
                {
                    this._volumePropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(VolumeWrapper));
                }

                return this._volumePropertytColumnDefinitions;
            }
        }

        private ICommand _viewSystemLog;

        public ICommand ViewSystemLog
        {
            get => _viewSystemLog;
            set => SetProperty(ref _viewSystemLog, value);
        }

        private ICommand _createImage;

        public ICommand CreateImage
        {
            get => _createImage;
            set => SetProperty(ref _createImage, value);
        }

        private ICommand _changeTerminationProtection;

        public ICommand ChangeTerminationProtection
        {
            get => _changeTerminationProtection;
            set => SetProperty(ref _changeTerminationProtection, value);
        }

        private ICommand _changeUserData;

        public ICommand ChangeUserData
        {
            get => _changeUserData;
            set => SetProperty(ref _changeUserData, value);
        }

        private ICommand _changeInstanceType;

        public ICommand ChangeInstanceType
        {
            get => _changeInstanceType;
            set => SetProperty(ref _changeInstanceType, value);
        }
    }
}
