using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class CreateRoleModel : BaseModel
    {
        string _roleName;
        public string RoleName
        {
            get => this._roleName;
            set
            {
                this._roleName = value;
                base.NotifyPropertyChanged("RoleName");
            }
        }
    }
}
