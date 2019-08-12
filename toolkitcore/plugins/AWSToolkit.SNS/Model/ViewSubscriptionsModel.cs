using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class ViewSubscriptionsModel : BaseModel
    {
        string _owningTopicARN;
        ObservableCollection<SubscriptionEntry> _subscriptionEntries = new ObservableCollection<SubscriptionEntry>();

        public string OwningTopicARN
        {
            get => this._owningTopicARN;
            set
            {
                this._owningTopicARN = value;
                NotifyPropertyChanged("OwningTopicARN");
            }
        }

        public ObservableCollection<SubscriptionEntry> SubscriptionEntries
        {
            get => this._subscriptionEntries;
            set
            {
                this._subscriptionEntries = value;
                base.NotifyPropertyChanged("SubscriptionEntries");
            }
        }
    }
}
