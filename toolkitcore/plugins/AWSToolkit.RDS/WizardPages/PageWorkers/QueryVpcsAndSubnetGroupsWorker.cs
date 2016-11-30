using System;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageWorkers
{
    internal class QueryVpcsAndSubnetGroupsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<Vpc> vpcs, IEnumerable<DBSubnetGroup> dbSubnetGroups);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public IAmazonRDS RDSClient { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for the vpcs and corresponding db subnet groups available to the user
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="rdsClient"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryVpcsAndSubnetGroupsWorker(IAmazonEC2 ec2Client,
                                              IAmazonRDS rdsClient, 
                                              ILog logger,
                                              DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                EC2Client = ec2Client,
                RDSClient = rdsClient,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var vpcs = new List<Vpc>();
            var dbSubnetGroups = new List<DBSubnetGroup>();

            try
            {
                var vpcResponse = workerData.EC2Client.DescribeVpcs();
                vpcs.AddRange(vpcResponse.Vpcs);

                var response = workerData.RDSClient.DescribeDBSubnetGroups();
                dbSubnetGroups.AddRange(response.DBSubnetGroups);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { vpcs, dbSubnetGroups };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as IEnumerable<Vpc>, results[1] as IEnumerable<DBSubnetGroup>);
            }
        }

        private QueryVpcsAndSubnetGroupsWorker() { }
    }
}
