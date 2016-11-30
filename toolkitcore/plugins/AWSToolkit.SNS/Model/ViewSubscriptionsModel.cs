using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class ViewSubscriptionsModel : BaseModel
    {
        string _owningTopicARN;
        ObservableCollection<SubscriptionEntry> _subscriptionEntries = new ObservableCollection<SubscriptionEntry>();

        public string OwningTopicARN
        {
            get { return this._owningTopicARN; }
            set
            {
                this._owningTopicARN = value;
                NotifyPropertyChanged("OwningTopicARN");
            }
        }

        public ObservableCollection<SubscriptionEntry> SubscriptionEntries
        {
            get { return this._subscriptionEntries; }
            set
            {
                this._subscriptionEntries = value;
                base.NotifyPropertyChanged("SubscriptionEntries");
            }
        }
    }
}
