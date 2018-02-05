using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.SQS;
using Amazon.SQS.Model;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using log4net;
using System.ComponentModel;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QueryDLQTargetsWorker
    {
        public delegate void DataAvailableCallback(QueryResults results);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonSimpleNotificationService SNSClient { get; set; }
            public IAmazonSQS SQSClient { get; set; }
            public ILog Logger { get; set; }
        }

        public class QueryResults
        {
            public IList<string> TopicArns { get; set; }

            public string TopicsErrorMessage {get;set;}


            public IList<string> QueueArns { get; set; }

            public string QueuesErrorMessage { get; set; }
        }

        public QueryDLQTargetsWorker(IAmazonSimpleNotificationService snsClient,
                                 IAmazonSQS sqsClient,
                                 ILog logger,
                                 DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData { SNSClient = snsClient, SQSClient = sqsClient, Logger = logger });
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as QueryResults);
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var wd = (WorkerData)e.Argument;

            var results = new QueryResults();

            try
            {
                var response = wd.SNSClient.ListTopics(new ListTopicsRequest());
                results.TopicArns = new List<string>();
                response.Topics.ForEach(x => results.TopicArns.Add(x.TopicArn));
            }
            catch (Exception exc)
            {
                wd.Logger.Error(GetType().FullName + ", exception in Query", exc);
                results.TopicsErrorMessage = "Error listing SNS topic: " + exc.Message;
            }

            string region = Amazon.Util.AWSSDKUtils.DetermineRegion(wd.SQSClient.Config.DetermineServiceURL());
            try
            {
                var response = wd.SQSClient.ListQueues(new ListQueuesRequest());
                results.QueueArns = new List<string>();
                response.QueueUrls.ForEach(x => results.QueueArns.Add(wd.SQSClient.GetQueueARN(region,  x)));
            }
            catch (Exception exc)
            {
                wd.Logger.Error(GetType().FullName + ", exception in Query", exc);
                results.QueuesErrorMessage = "Error listing SQS Queues: " + exc.Message;
            }

            e.Result = results;
        }
    }
}
