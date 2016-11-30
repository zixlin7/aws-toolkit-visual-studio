using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.EC2.Model
{
    public class ChooseInstanceToConnectModel : BaseModel
    {

        IList<string> _instanceIds;
        public IList<string> InstanceIds
        {
            get { return this._instanceIds; }
            set
            {
                this._instanceIds = value;
                base.NotifyPropertyChanged("InstanceIds");
            }
        }

        string _selectedInstance;
        public string SelectedInstance
        {
            get { return this._selectedInstance; }
            set
            {
                this._selectedInstance = value;
                base.NotifyPropertyChanged("SelectedInstance");
            }
        }
    }
}
