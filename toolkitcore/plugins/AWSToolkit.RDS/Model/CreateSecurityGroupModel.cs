using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class CreateSecurityGroupModel : BaseModel
    {
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
    }
}
