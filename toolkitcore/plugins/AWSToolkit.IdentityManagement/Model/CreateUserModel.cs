using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class CreateUserModel : BaseModel
    {
        string _userName;
        public string UserName
        {
            get => this._userName;
            set
            {
                this._userName = value;
                base.NotifyPropertyChanged("UserName");
            }
        }
    }
}
