using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using log4net;


namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QueryCloudFormationStacksWorker
    {
        public delegate void DataAvailableCallback(ICollection<StackSummary> data);
        DataAvailableCallback _callback;

        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        /// <param name="client"></param>
        public static List<StackSummary> FetchStacks(IAmazonCloudFormation client, ILog logger)
        {
            List<StackSummary> stacks = new List<StackSummary>();

            QueryCloudFormationStacksWorker workerObj = new QueryCloudFormationStacksWorker();
            workerObj.Query(client, logger, stacks);
            return stacks;
        }

        /// <summary>
        /// Perform async fetch
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryCloudFormationStacksWorker(IAmazonCloudFormation client,
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
            IAmazonCloudFormation client = args[0] as IAmazonCloudFormation;
            ILog logger = args[1] as ILog;

            List<StackSummary> stacks = new List<StackSummary>();
            Query(client, logger, stacks);
            e.Result = stacks;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<StackSummary>);
        }

        void Query(IAmazonCloudFormation client, ILog logger, List<StackSummary> stacks)
        {
            try
            {
                ListStacksResponse response = null;
                ListStacksRequest request = new ListStacksRequest();
                request.StackStatusFilter.AddRange(new []
                { 
                    "CREATE_COMPLETE", 
                    "UPDATE_COMPLETE",
                    "ROLLBACK_COMPLETE",
                    "UPDATE_ROLLBACK_COMPLETE"
                });
                do
                {
                    if (response != null)
                        request.NextToken = response.NextToken;

                    response = client.ListStacks(request);
                    stacks.AddRange(response.StackSummaries);
                } while (!string.IsNullOrEmpty(response.NextToken));
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Query", exc);
            }
        }

        private QueryCloudFormationStacksWorker() { }
    }
}
