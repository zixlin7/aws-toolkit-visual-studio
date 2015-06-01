using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class CreateRoleModel : BaseModel
    {
        string _roleName;
        public string RoleName
        {
            get { return this._roleName; }
            set
            {
                this._roleName = value;
                base.NotifyPropertyChanged("RoleName");
            }
        }
    }
}
