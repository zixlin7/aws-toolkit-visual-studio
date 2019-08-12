using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using log4net;


namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QueryTopicArnsWorker
    {
        public delegate void DataAvailableCallback(ICollection<string> data);
        DataAvailableCallback _callback;

        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        /// <param name="client"></param>
        public static List<string> FetchTopicArns(IAmazonSimpleNotificationService client, ILog logger)
        {
            List<string> arns = new List<string>();

            QueryTopicArnsWorker workerObj = new QueryTopicArnsWorker();
            workerObj.Query(client, logger, arns);
            return arns;
        }

                /// <summary>
        /// Perform async fetch
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryTopicArnsWorker(IAmazonSimpleNotificationService client,
                                         ILog logger,
                                         DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { client, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            var client = args[0] as IAmazonSimpleNotificationService;
            ILog logger = args[1] as ILog;

            List<string> arns = new List<string>();
            Query(client, logger, arns);
            e.Result = arns;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<string>);
        }

        void Query(IAmazonSimpleNotificationService client, ILog logger, List<string> arns)
        {
            try
            {
                var response = client.ListTopics(new ListTopicsRequest());                
                response.Topics.ForEach(x => arns.Add(x.TopicArn));
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Query", exc);
            }
        }

        private QueryTopicArnsWorker() { }
    }
}
