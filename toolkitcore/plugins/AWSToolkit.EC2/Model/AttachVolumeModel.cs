using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AttachVolumeModel : BaseModel
    {
        string _volumeId;
        public string VolumeId
        {
            get { return _volumeId; }
            set
            {
                _volumeId = value;
                base.NotifyPropertyChanged("VolumeId");
            }
        }

        IList<RunningInstanceWrapper> _availableInstances;
        public IList<RunningInstanceWrapper> AvailableInstances
        {
            get { return _availableInstances; }
            set
            {
                _availableInstances = value;
                base.NotifyPropertyChanged("AvailableInstances");
            }
        }

        RunningInstanceWrapper _instance;
        public RunningInstanceWrapper Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                base.NotifyPropertyChanged("Instance");
            }
        }

        string _device;
        public string Device
        {
            get { return _device; }
            set
            {
                _device = value;
                base.NotifyPropertyChanged("Device");
            }
        }


    }
}
