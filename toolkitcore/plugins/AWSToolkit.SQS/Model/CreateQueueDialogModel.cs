using System.Collections.ObjectModel;
using Amazon.SQS;

namespace Amazon.AWSToolkit.SQS.Model
{
    public class CreateQueueControlModel
    {
        internal const int DEFAULT_VISIBLITY_TIMEOUT = 30;
        internal const int MAX_DEFAULT_VISIBLITY_TIMEOUT = 43200;
        internal const int MIN_DEFAULT_VISIBLITY_TIMEOUT = 0;

        internal const int DEFAULT_MAX_RECEIVES = 1;
        internal const int MAX_DEFAULT_MAX_RECEIVES = 1000;
        internal const int MIN_DEFAULT_MAX_RECEIVES = 1;

        public CreateQueueControlModel()
        {
            this.DefaultVisiblityTimeout = DEFAULT_VISIBLITY_TIMEOUT;
            this.MaxReceives = DEFAULT_MAX_RECEIVES;
        }

        public IAmazonSQS SQSClient
        {
            get;
            set;
        }

        public string QueueName
        {
            get;
            set;
        }

        public int DefaultVisiblityTimeout
        {
            get;
            set;
        }

        public int DefaultDelaySeconds
        {
            get;
            set;
        }

        public bool UseRedrivePolicy
        {
            get;
            set;
        }

        public string DeadLetterQueueUrl
        {
            get;
            set;
        }

        readonly ObservableCollection<QueueViewBaseModel> _existingQueues = new ObservableCollection<QueueViewBaseModel>();
        internal ObservableCollection<QueueViewBaseModel> ExistingQueues => _existingQueues;

        public int MaxReceives
        {
            get;
            set;
        }
    }
}
