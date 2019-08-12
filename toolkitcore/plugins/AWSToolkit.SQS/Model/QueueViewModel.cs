using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.SQS.Model
{
    /// <summary>
    /// Lightweight queue model handling just name, url and arn components that we use
    /// in various locations
    /// </summary>
    public class QueueViewBaseModel : BaseModel
    {
        string _name;
        public string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                base.NotifyPropertyChanged("Name");
            }
        }

        string _queueARN;
        public string QueueARN
        {
            get => this._queueARN;
            set
            {
                this._queueARN = value;
                base.NotifyPropertyChanged("QueueARN");
            }
        }

        string _queueURL;
        public string QueueURL
        {
            get => this._queueURL;
            set
            {
                this._queueURL = value;
                base.NotifyPropertyChanged("QueueURL");
            }
        }

    }

    public class QueueViewModel : QueueViewBaseModel
    {
        readonly ObservableCollection<MessageWrapper> _messages = new ObservableCollection<MessageWrapper>();
        public ObservableCollection<MessageWrapper> Messages => this._messages;

        public bool IsDirty()
        {
            if (this.OrignalMaximumMessageSize != this.MaximumMessageSize)
                return true;
            if (this.OrignalMessageRetentionPeriod != MessageRetentionPeriod)
                return true;
            if (this.OrignalVisibilityTimeout != this.VisibilityTimeout)
                return true;
            if (this.OrignalDelaySeconds != this.DelaySeconds)
                return true;

            return false;
        }

        int _visibilityTimeout;
        public int VisibilityTimeout 
        {
            get => this._visibilityTimeout;
            set
            {
                this._visibilityTimeout = value;
                base.NotifyPropertyChanged("VisibilityTimeout");
            }
        }

        public int OrignalVisibilityTimeout { get; set; }

        int _delaySeconds;
        public int DelaySeconds
        {
            get => this._delaySeconds;
            set
            {
                this._delaySeconds = value;
                base.NotifyPropertyChanged("DelaySeconds");
            }
        }

        public int OrignalDelaySeconds { get; set; }
        

        int _maximumMessageSize;
        public int MaximumMessageSize
        {
            get => this._maximumMessageSize;
            set
            {
                this._maximumMessageSize = value;
                base.NotifyPropertyChanged("MaximumMessageSize");
            }
        }

        public int OrignalMaximumMessageSize { get; set; }

        int _messageRetentionPeriod;
        public int MessageRetentionPeriod
        {
            get => this._messageRetentionPeriod;
            set
            {
                this._messageRetentionPeriod = value;
                base.NotifyPropertyChanged("MessageRetentionPeriod");
            }
        }

        public int OrignalMessageRetentionPeriod { get; set; }

        int _approximateNumberOfMessages;
        public int ApproximateNumberOfMessages
        {
            get => this._approximateNumberOfMessages;
            set
            {
                this._approximateNumberOfMessages = value;
                base.NotifyPropertyChanged("ApproximateNumberOfMessages");
            }
        }

        int _approximateNumberOfMessagesNotVisible;
        public int ApproximateNumberOfMessagesNotVisible
        {
            get => this._approximateNumberOfMessagesNotVisible;
            set
            {
                this._approximateNumberOfMessagesNotVisible = value;
                base.NotifyPropertyChanged("ApproximateNumberOfMessagesNotVisible");
            }
        }

        DateTime _createTimestamp;
        public DateTime CreatedTimestamp
        {
            get => this._createTimestamp;
            set
            {
                this._createTimestamp = value;
                base.NotifyPropertyChanged("CreatedTimestamp");
            }
        }

        DateTime _lastModifiedTimestamp;
        public DateTime LastModifiedTimestamp
        {
            get => this._lastModifiedTimestamp;
            set
            {
                this._lastModifiedTimestamp = value;
                base.NotifyPropertyChanged("LastModifiedTimestamp");
            }
        }

        internal void SetRedrivePolicy(string policy)
        {
            HasRedrivePolicy = !string.IsNullOrEmpty(policy);    
            if (HasRedrivePolicy)
            {
                var jdata = JsonMapper.ToObject(policy);
                MaximumReceives = (int)jdata["maxReceiveCount"];
                DeadLetterQueue = (string)jdata["deadLetterTargetArn"];
            }
        }

        public bool HasRedrivePolicy { get; private set; }

        int _maximumReceives;
        public int MaximumReceives
        {
            get => HasRedrivePolicy ? _maximumReceives : 0;
            private set
            {
                _maximumReceives = value;
                base.NotifyPropertyChanged("MaximumReceives");
            }
        }

        string _deadLetterQueue;
        public string DeadLetterQueue
        {
            get => HasRedrivePolicy ? _deadLetterQueue : string.Empty;
            private set
            {
                _deadLetterQueue = value;
                base.NotifyPropertyChanged("DeadLetterQueue");
            }
        }

        internal void SetRedriveTarget(IEnumerable<QueueViewBaseModel> sourceQueues)
        {
            IsRedriveTarget = sourceQueues.Any();            
            if (IsRedriveTarget)
            {
                DeadLetterSourceQueues = sourceQueues;    
            }
        }

        bool _isRedriveTarget = false;
        public bool IsRedriveTarget
        {
            get => _isRedriveTarget;
            set
            {
                this._isRedriveTarget = value;
                base.NotifyPropertyChanged("IsRedriveTarget");
            }
        }

        IEnumerable<QueueViewBaseModel> _deadLetterSourceQueues;
        public IEnumerable<QueueViewBaseModel> DeadLetterSourceQueues
        {
            get => _deadLetterSourceQueues;
            private set
            {
                _deadLetterSourceQueues = value;
                base.NotifyPropertyChanged("DeadLetterSourceQueues");
            }
        }
    }
}
