using System;
using System.Collections.Generic;
using System.ComponentModel;

using log4net;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Workers
{
    /// <summary>
    /// Worker used to fetch the set of queues owned by the user in a specific region
    /// </summary>
    internal class QueryExistingQueuesWorker
    {
        public delegate void DataAvailableCallback(ICollection<string> data);
        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonSQS SQSClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryExistingQueuesWorker(IAmazonSQS sqsClient,
                                         ILog logger,
                                         DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                SQSClient = sqsClient,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var queues = new List<string>();

            try
            {
                var request = new ListQueuesRequest();
                var response = workerData.SQSClient.ListQueues(request);
                queues.AddRange(response.QueueUrls);
            }
            catch (Exception ex)
            {
                workerData.Logger.Warn("Unable to list queues: " + ex.Message);
            }

            e.Result = queues;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<string>);
        }

        private QueryExistingQueuesWorker() { }
    }
}
