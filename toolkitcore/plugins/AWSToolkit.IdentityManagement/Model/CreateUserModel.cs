using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class CreateUserModel : BaseModel
    {
        string _userName;
        public string UserName
        {
            get { return this._userName; }
            set
            {
                this._userName = value;
                base.NotifyPropertyChanged("UserName");
            }
        }
    }
}
