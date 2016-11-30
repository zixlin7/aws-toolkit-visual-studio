using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Amazon.RDS;
using Amazon.RDS.Model;

using Amazon.AWSToolkit.Account;

using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageWorkers
{
    internal class QueryDBParameterGroupsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<DBParameterGroup> dbParameterGroups);
        DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonRDS RDSClient { get; set; }
            public DBEngineVersion EngineVersion { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for db parameter groups available to the user
        /// </summary>
        /// <param name="rdsClient"></param>
        /// <param name="forFamily"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryDBParameterGroupsWorker(IAmazonRDS rdsClient,
                                            DBEngineVersion engineVersion,
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
                EngineVersion = engineVersion,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            List<DBParameterGroup> dbParameterGroups = new List<DBParameterGroup>();

            try
            {
                var response = workerData.RDSClient.DescribeDBParameterGroups();
                if (workerData.EngineVersion == null)
                    dbParameterGroups.AddRange(response.DBParameterGroups);
                else
                    foreach (DBParameterGroup group in response.DBParameterGroups)
                    {
                        if (string.Compare(workerData.EngineVersion.DBParameterGroupFamily,
                                            group.DBParameterGroupFamily,
                                            StringComparison.InvariantCultureIgnoreCase) == 0)
                            dbParameterGroups.Add(group);
                    }
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { dbParameterGroups };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                object[] results = e.Result as object[];
                _callback(results[0] as List<DBParameterGroup>);
            }
        }

        private QueryDBParameterGroupsWorker() { }
    }
}
