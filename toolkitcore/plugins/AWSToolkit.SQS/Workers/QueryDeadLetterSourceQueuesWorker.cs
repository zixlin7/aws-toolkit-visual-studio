using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.AWSToolkit.SQS.Model;
using log4net;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Workers
{
    /// <summary>
    /// Worker used to probe existing queues to see if any are source queues for the
    /// current queue (ie the current queue is the target of a redrive policy on the
    /// source queues).
    /// </summary>
    internal class QueryDeadLetterSourceQueuesWorker
    {
        public delegate void DataAvailableCallback(ICollection<QueueViewBaseModel> data);
        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonSQS SQSClient { get; set; }
            public string QueueUrl { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryDeadLetterSourceQueuesWorker(IAmazonSQS sqsClient,
                                                 string queueUrl,
                                                 ILog logger,
                                                 DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker
                {
                    WorkerReportsProgress = true, 
                    WorkerSupportsCancellation = true
                };

            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                SQSClient = sqsClient,
                QueueUrl = queueUrl,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var queues = new List<QueueViewBaseModel>();

            try
            {
                var request = new ListDeadLetterSourceQueuesRequest { QueueUrl = workerData.QueueUrl };
                var response = workerData.SQSClient.ListDeadLetterSourceQueues(request);
                foreach (var queueUrl in response.QueueUrls)
                {
                    try
                    {
                        var arnRequest = new GetQueueAttributesRequest
                        {
                            AttributeNames = { "QueueArn" },
                            QueueUrl = queueUrl
                        };
                        var arnResponse = workerData.SQSClient.GetQueueAttributes(arnRequest);
                        queues.Add(new QueueViewBaseModel
                            {
                                Name = queueUrl.Substring(queueUrl.LastIndexOf('/') + 1),
                                QueueURL = queueUrl,
                                QueueARN = arnResponse.QueueARN
                            });
                    }
                    catch (Exception ex)
                    {
                        workerData.Logger.Warn("Unable to obtain QueueArn attribute for queue url " + queueUrl + ", exception: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                workerData.Logger.Warn("Unable to list dead letter queues against queue url " + workerData.QueueUrl + ", exception: " + ex.Message);
            }

            e.Result = queues;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<QueueViewBaseModel>);
        }

        private QueryDeadLetterSourceQueuesWorker() { }
    }
}
