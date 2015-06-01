using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class GetConsoleOutputModel : BaseModel
    {

        string _instanceId;
        public string InstanceId
        {
            get { return this._instanceId; }
            set
            {
                this._instanceId = value;
                base.NotifyPropertyChanged("InstanceId");
            }
        }

        DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return this._timestamp; }
            set
            {
                this._timestamp = value;
                base.NotifyPropertyChanged("Timestamp");
            }
        }

        string _consoleOutput;
        public string ConsoleOutput
        {
            get { return this._consoleOutput; }
            set
            {
                this._consoleOutput = value;
                base.NotifyPropertyChanged("ConsoleOutput");
            }
        }
    }
}
