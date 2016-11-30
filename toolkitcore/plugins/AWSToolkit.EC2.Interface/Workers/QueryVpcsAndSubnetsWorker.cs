using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Workers
{
    public class QueryVpcsAndSubnetsWorker
    {
        public delegate void DataAvailableCallback(ICollection<Vpc> vpcs, ICollection<Subnet> subnets);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for all VPCs and the related subnets available to the user
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryVpcsAndSubnetsWorker(IAmazonEC2 ec2Client, ILog logger, DataAvailableCallback callback)
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
            var subnets = new List<Subnet>();

            try
            {
                var vpcQueryResponse = workerData.EC2Client.DescribeVpcs();
                vpcs.AddRange(vpcQueryResponse.Vpcs);

                var subnetsQueryResponse = workerData.EC2Client.DescribeSubnets();
                subnets.AddRange(subnetsQueryResponse.Subnets);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { vpcs, subnets };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as List<Vpc>, results[1] as List<Subnet>);
            }
        }

        private QueryVpcsAndSubnetsWorker() { }
    }
}
