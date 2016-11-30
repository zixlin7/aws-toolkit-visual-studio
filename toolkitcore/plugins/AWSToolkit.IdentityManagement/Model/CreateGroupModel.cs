using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class CreateGroupModel : BaseModel
    {
        string _groupName;
        public string GroupName
        {
            get { return this._groupName; }
            set
            {
                this._groupName = value;
                base.NotifyPropertyChanged("GroupName");
            }
        }
    }
}
