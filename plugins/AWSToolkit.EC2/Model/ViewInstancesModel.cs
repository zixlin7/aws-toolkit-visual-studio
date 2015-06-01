using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewInstancesModel : BaseModel
    {
        ObservableCollection<RunningInstanceWrapper> _runningInstances = new ObservableCollection<RunningInstanceWrapper>();
        public ObservableCollection<RunningInstanceWrapper> RunningInstances
        {
            get { return this._runningInstances; }
        }

        IList<RunningInstanceWrapper> _selectedInstances = new List<RunningInstanceWrapper>();
        public IList<RunningInstanceWrapper> SelectedInstances
        {
            get { return this._selectedInstances; }
        }
       
        RunningInstanceWrapper _focusInstance;
        public RunningInstanceWrapper FocusInstance
        {
            get { return this._focusInstance; }
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


        public string[] ListInstanceAvailableTags
        {
            get
            {
                return EC2ColumnDefinition.GetListAvailableTags(this.RunningInstances);
            }
        }

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
    }
}
