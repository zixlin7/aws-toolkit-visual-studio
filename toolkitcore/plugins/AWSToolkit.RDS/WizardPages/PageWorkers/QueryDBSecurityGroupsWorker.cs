using System;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.RDS;
using Amazon.RDS.Model;
using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageWorkers
{
    internal class QueryDBSecurityGroupsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<DBSecurityGroup> dbSecurityGroups);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonRDS RDSClient { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for db security groups available to the user
        /// </summary>
        /// <param name="rdsClient"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryDBSecurityGroupsWorker(IAmazonRDS rdsClient, 
                                           ILog logger,
                                           DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                RDSClient = rdsClient,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var dbSecurityGroups = new List<DBSecurityGroup>();

            try
            {
                var response = workerData.RDSClient.DescribeDBSecurityGroups();
                dbSecurityGroups.AddRange(response.DBSecurityGroups);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { dbSecurityGroups };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as List<DBSecurityGroup>);
            }
        }

        private QueryDBSecurityGroupsWorker() { }
    }
}
