using System;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class GetConsoleOutputModel : BaseModel
    {

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

        DateTime _timestamp;
        public DateTime Timestamp
        {
            get => this._timestamp;
            set
            {
                this._timestamp = value;
                base.NotifyPropertyChanged("Timestamp");
            }
        }

        string _consoleOutput;
        public string ConsoleOutput
        {
            get => this._consoleOutput;
            set
            {
                this._consoleOutput = value;
                base.NotifyPropertyChanged("ConsoleOutput");
            }
        }
    }
}
