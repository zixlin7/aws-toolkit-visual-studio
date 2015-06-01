using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class CreateTopicModel : BaseModel
    {
        string _topicName;
        string _topicARN;


        public CreateTopicModel()
        {
        }

        public string TopicName
        {
            get { return this._topicName; }
            set
            {
                this._topicName = value;
                this.NotifyPropertyChanged("TopicName");
            }
        }

        public string TopicARN
        {
            get { return this._topicARN; }
            set
            {
                this._topicARN = value;
                base.NotifyPropertyChanged("TopicARN");
            }
        }
    }
}
