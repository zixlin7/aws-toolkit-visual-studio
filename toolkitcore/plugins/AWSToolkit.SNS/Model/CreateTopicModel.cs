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
            get => this._topicName;
            set
            {
                this._topicName = value;
                this.NotifyPropertyChanged("TopicName");
            }
        }

        public string TopicARN
        {
            get => this._topicARN;
            set
            {
                this._topicARN = value;
                base.NotifyPropertyChanged("TopicARN");
            }
        }
    }
}
