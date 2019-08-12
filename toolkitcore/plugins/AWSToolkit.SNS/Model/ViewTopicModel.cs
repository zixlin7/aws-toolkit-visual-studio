using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class ViewTopicModel : BaseModel
    {
        string _topicARN;
        string _topicOwner;
        string _displayName;

        ViewSubscriptionsModel _subscriptionModel = new ViewSubscriptionsModel();

        public string TopicARN
        {
            get => this._topicARN;
            set
            {
                this._topicARN = value;
                this.NotifyPropertyChanged("TopicArn");
            }
        }

        public string TopicOwner
        {
            get => string.Format("AWS Account Number {0}", this._topicOwner);
            set
            {
                this._topicOwner = value;
                this.NotifyPropertyChanged("TopicOwner");
            }
        }


        public string DisplayName
        {
            get => this._displayName;
            set
            {
                this._displayName = value;
                this.NotifyPropertyChanged("DisplayName");
            }
        }

        public ViewSubscriptionsModel SubscriptionModel
        {
            get => this._subscriptionModel;
            set
            {
                this._subscriptionModel = value;
                this.NotifyPropertyChanged("SubscriptionModel");
            }
        }
    }
}
