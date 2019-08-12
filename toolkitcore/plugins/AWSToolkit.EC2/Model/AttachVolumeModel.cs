using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AttachVolumeModel : BaseModel
    {
        string _volumeId;
        public string VolumeId
        {
            get => _volumeId;
            set
            {
                _volumeId = value;
                base.NotifyPropertyChanged("VolumeId");
            }
        }

        IList<RunningInstanceWrapper> _availableInstances;
        public IList<RunningInstanceWrapper> AvailableInstances
        {
            get => _availableInstances;
            set
            {
                _availableInstances = value;
                base.NotifyPropertyChanged("AvailableInstances");
            }
        }

        RunningInstanceWrapper _instance;
        public RunningInstanceWrapper Instance
        {
            get => _instance;
            set
            {
                _instance = value;
                base.NotifyPropertyChanged("Instance");
            }
        }

        string _device;
        public string Device
        {
            get => _device;
            set
            {
                _device = value;
                base.NotifyPropertyChanged("Device");
            }
        }


    }
}
