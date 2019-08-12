using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class PublishModel : BaseModel
    {
        string _topicARN;
        string _subject;
        string _message;

        public PublishModel(string topicARN)
        {
            this._topicARN = topicARN;
        }

        public string TopicARN => this._topicARN;

        public string Subject
        {
            get => this._subject;
            set
            {
                this._subject = value;
                base.NotifyPropertyChanged("Subject");
            }
        }

        public string Message
        {
            get => this._message;
            set
            {
                this._message = value;
                base.NotifyPropertyChanged("Message");
            }
        }
    }
}
