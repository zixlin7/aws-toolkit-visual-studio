using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.View;
using Amazon.AWSToolkit.SQS.Model;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.SQS.Workers;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;

using log4net;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class QueueViewCommand : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(QueueViewCommand));

        private const int NUMBER_OF_FETCH_REQUESTS = 5;

        readonly Dictionary<string, CachedAttributeValue> _cachedValues = new Dictionary<string,CachedAttributeValue>();

        IAmazonSQS _sqsClient;
        QueueViewModel _queueViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            var queueModel = model as SQSQueueViewModel;
            if (queueModel == null)
                return new ActionResults().WithSuccess(false);

            this._sqsClient = queueModel.SQSClient;
            this._queueViewModel = new QueueViewModel
                {
                    Name = queueModel.Name,
                    QueueARN = queueModel.QueueARN,
                    QueueURL = queueModel.QueueUrl
                };

            var view = new QueueViewControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(view);
            return new ActionResults()
                .WithSuccess(true);
        }

        public QueueViewModel Model
        {
            get { return this._queueViewModel; }
        }

        public void LoadModel()
        {
            Refresh();
        }

        public void Refresh()
        {
            RefreshAttributes();
            RefreshMessageSampling();
        }

        private void RefreshMessageSampling()
        {
            var cached = new Dictionary<string, Message>();
            var messages = new List<MessageWrapper>();

            for (int i = 0; i < NUMBER_OF_FETCH_REQUESTS; i++)
            {
                var response = this._sqsClient.ReceiveMessage(new ReceiveMessageRequest()
                {
                    QueueUrl = this._queueViewModel.QueueURL,
                    AttributeNames = new List<string>() { "All" },
                    VisibilityTimeout = 1,
                    MaxNumberOfMessages = 10
                });

                foreach (Message message in response.Messages)
                {
                    if (!cached.ContainsKey(message.MessageId))
                    {
                        messages.Add(new MessageWrapper(message));
                    }
                    cached[message.MessageId] = message;
                }
            }

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    this._queueViewModel.Messages.Clear();
                    foreach (var message in messages)
                    {
                        this._queueViewModel.Messages.Add(message);
                    }
                }));
        }

        private void RefreshAttributes()
        {
            var response = this._sqsClient.GetQueueAttributes(new GetQueueAttributesRequest()
            {
                AttributeNames = new List<string>() { "All" },
                QueueUrl = this._queueViewModel.QueueURL
            });

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    int timeout;
                    if (this.UseCacheValue(SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT))
                        timeout = this._cachedValues[SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT].Value;
                    else
                        timeout = response.VisibilityTimeout;

                    this._queueViewModel.VisibilityTimeout = timeout;
                    this._queueViewModel.OrignalVisibilityTimeout = this._queueViewModel.VisibilityTimeout;

                    int messageSize;
                    if (this.UseCacheValue(SQSConstants.ATTRIBUTE_MAXIMUM_MESSAGE_SIZE))
                        messageSize = this._cachedValues[SQSConstants.ATTRIBUTE_MAXIMUM_MESSAGE_SIZE].Value;
                    else
                        messageSize = response.MaximumMessageSize;

                    int delaySeconds;
                    if (this.UseCacheValue(SQSConstants.ATTRIBUTE_DELAY_SECONDS))
                        delaySeconds = this._cachedValues[SQSConstants.ATTRIBUTE_DELAY_SECONDS].Value;
                    else
                        delaySeconds = response.DelaySeconds;
                    
                    this._queueViewModel.DelaySeconds = delaySeconds;
                    this._queueViewModel.OrignalDelaySeconds = this._queueViewModel.DelaySeconds;


                    this._queueViewModel.MaximumMessageSize = messageSize;
                    this._queueViewModel.OrignalMaximumMessageSize = this._queueViewModel.MaximumMessageSize;

                    int retentionPeriod;
                    if (this.UseCacheValue(SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD))
                        retentionPeriod = this._cachedValues[SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD].Value;
                    else
                        retentionPeriod = response.MessageRetentionPeriod;

                    this._queueViewModel.MessageRetentionPeriod = retentionPeriod;
                    this._queueViewModel.OrignalMessageRetentionPeriod = this._queueViewModel.MessageRetentionPeriod;

                    this._queueViewModel.ApproximateNumberOfMessages = response.ApproximateNumberOfMessages;
                    this._queueViewModel.ApproximateNumberOfMessagesNotVisible = response.ApproximateNumberOfMessagesNotVisible;
                    this._queueViewModel.CreatedTimestamp = response.CreatedTimestamp;
                    this._queueViewModel.LastModifiedTimestamp = response.LastModifiedTimestamp;

                    // don't believe its possible for a target of a redrive policy to itself be a source
                    string redrivePolicy;
                    if (response.Attributes.TryGetValue(QueueAttributeName.RedrivePolicy, out redrivePolicy))
                    {
                        this._queueViewModel.SetRedrivePolicy(redrivePolicy);
                    }
                    else
                    {
                        new QueryDeadLetterSourceQueuesWorker(this._sqsClient, 
                                                              this._queueViewModel.QueueURL, 
                                                              LOGGER, 
                                                              data => this._queueViewModel.SetRedriveTarget(data));
                    }
                }));
        }

        public void SaveAttributes()
        {
            if (this._queueViewModel.OrignalMaximumMessageSize != this._queueViewModel.MaximumMessageSize)
            {
                SaveQueueAttribute("Visibility timeout", SQSConstants.ATTRIBUTE_MAXIMUM_MESSAGE_SIZE, this._queueViewModel.MaximumMessageSize);
                this._queueViewModel.OrignalMaximumMessageSize = this._queueViewModel.MaximumMessageSize;
            }
            if (this._queueViewModel.OrignalMessageRetentionPeriod != this._queueViewModel.MessageRetentionPeriod)
            {
                SaveQueueAttribute("Maximum message size", SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD, this._queueViewModel.MessageRetentionPeriod);
                this._queueViewModel.OrignalMessageRetentionPeriod = this._queueViewModel.MessageRetentionPeriod;
            }
            if (this._queueViewModel.OrignalVisibilityTimeout != this._queueViewModel.VisibilityTimeout)
            {
                SaveQueueAttribute("Message retention period", SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT, this._queueViewModel.VisibilityTimeout);
                this._queueViewModel.OrignalVisibilityTimeout = this._queueViewModel.VisibilityTimeout;
            }
            if (this._queueViewModel.OrignalDelaySeconds != this._queueViewModel.DelaySeconds)
            {
                SaveQueueAttribute("Delay in Seconds", SQSConstants.ATTRIBUTE_DELAY_SECONDS, this._queueViewModel.DelaySeconds);
                this._queueViewModel.OrignalDelaySeconds = this._queueViewModel.DelaySeconds;
            }
        }

        private void SaveQueueAttribute(string attributeFriendlyName, string name, object value)
        {
            try
            {
                this._sqsClient.SetQueueAttribute(this._queueViewModel.QueueURL, name, value != null ? value.ToString() : string.Empty);
                this._cachedValues[name] = new CachedAttributeValue(Convert.ToInt32(value), DateTime.Now);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error setting attribute {0}: {1} ", attributeFriendlyName, e.Message));
                throw;
            }

        }

        public bool SendMessage()
        {
            var details = new NewMessageDetails();
            bool dialogResult = ToolkitFactory.Instance.ShellProvider.ShowModal(details);
            if (dialogResult)
            {
                var request = new SendMessageRequest()
                {
                    MessageBody = details.MessageBodyContent,
                    QueueUrl = this._queueViewModel.QueueURL
                };

                if (details.DelaySeconds.HasValue && details.DelaySeconds >= 0)
                    request.DelaySeconds = details.DelaySeconds.Value;

                this._sqsClient.SendMessage(request);

                return true;
            }

            
            return false;
        }

        public bool PurgeQueue()
        {
            var shouldDelete = ToolkitFactory.Instance.ShellProvider.Confirm("Purge Queue",
                "Are you sure you want to purge the queue (removing all the messages left in it)?");

            if (shouldDelete)
            {
                var request = new PurgeQueueRequest
                {
                    QueueUrl = this._queueViewModel.QueueURL
                };

                this._sqsClient.PurgeQueue(request);
                return true;
            }

            return false;
        }

        bool UseCacheValue(string name)
        {
            if (!this._cachedValues.ContainsKey(name))
                return false;

            var ca = this._cachedValues[name];
            return ca.Timestamp > DateTime.Now.AddSeconds(-90);
        }

        public class CachedAttributeValue
        {
            public CachedAttributeValue(int value, DateTime timestamp)
            {
                this.Value = value;
                this.Timestamp = timestamp;
            }

            public int Value { get; private set; }
            public DateTime Timestamp {get;private set;}
        }
    }
}
