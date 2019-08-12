using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateImageModel : BaseModel
    {

        public CreateImageModel(string instanceId)
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

        string _name;
        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                base.NotifyPropertyChanged("Name");
            }
        }

        string _description;
        public string Description
        {
            get => this._description;
            set
            {
                this._description = value;
                base.NotifyPropertyChanged("Description");
            }
        }
    }
}
