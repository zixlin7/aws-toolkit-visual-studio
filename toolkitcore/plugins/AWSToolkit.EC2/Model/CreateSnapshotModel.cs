using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateSnapshotModel : BaseModel
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

        string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                base.NotifyPropertyChanged("Name");
            }
        }
    }
}
