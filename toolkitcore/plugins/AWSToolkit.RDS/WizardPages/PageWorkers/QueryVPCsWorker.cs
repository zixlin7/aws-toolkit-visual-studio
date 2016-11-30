using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageWorkers
{
    internal class QueryVpcsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<Vpc> vpcs);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for all VPCs available to the user
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryVpcsWorker(IAmazonEC2 ec2Client, ILog logger, DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
            {
                EC2Client = ec2Client,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var vpcs = new List<Vpc>();

            try
            {
                var response = workerData.EC2Client.DescribeVpcs();
                vpcs.AddRange(response.Vpcs);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { vpcs };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as List<Vpc>);
            }
        }

        private QueryVpcsWorker() { }
    }
}
