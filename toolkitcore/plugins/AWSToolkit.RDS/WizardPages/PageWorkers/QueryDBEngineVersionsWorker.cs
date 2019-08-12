using System;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageWorkers
{
    internal class QueryDBEngineVersionsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<DBEngineVersion> dbEngineVersions);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonRDS RDSClient { get; set; }
            public string DBEngine { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for engine versions, optionally tied to
        /// a given engine.
        /// </summary>
        /// <param name="rdsClient"></param>
        /// <param name="dbEngine"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryDBEngineVersionsWorker(IAmazonRDS rdsClient, 
                                           string dbEngine,
                                           ILog logger,
                                           DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new WorkerData
            {
                RDSClient = rdsClient,
                DBEngine = dbEngine,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var dbEngineVersions = new List<DBEngineVersion>();

            try
            {
                var request = new DescribeDBEngineVersionsRequest();
                if (!string.IsNullOrEmpty(workerData.DBEngine))
                    request.Engine = workerData.DBEngine;
                var response = workerData.RDSClient.DescribeDBEngineVersions(request);
                dbEngineVersions.AddRange(response.DBEngineVersions);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { dbEngineVersions };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                object[] results = e.Result as object[];
                _callback(results[0] as List<DBEngineVersion>);
            }
        }

        private QueryDBEngineVersionsWorker() { }
    }
}
