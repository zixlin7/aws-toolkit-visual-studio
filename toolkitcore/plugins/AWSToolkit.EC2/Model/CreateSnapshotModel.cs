using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateSnapshotModel : BaseModel
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

        string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                base.NotifyPropertyChanged("Name");
            }
        }
    }
}
