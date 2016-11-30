using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amazon.EC2;
using Amazon.EC2.Model;
using log4net;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageWorkers
{
    internal class QueryVPCSecurityGroupsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<SecurityGroup> dbSecurityGroups);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public string VpcId { get; set; }
            public ILog Logger { get; set; }
        }

        /// <summary>
        /// Do an asynchronous query for vpc-based security groups available to the user
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="vpcId"></param>
        /// <param name="logger"></param>
        /// <param name="callback"></param>
        public QueryVPCSecurityGroupsWorker(IAmazonEC2 ec2Client,
                                            string vpcId,
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
                VpcId = vpcId,
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var securityGroups = new List<SecurityGroup>();

            try
            {
                var request = new DescribeSecurityGroupsRequest
                {
                    Filters = new List<Filter>
                    {
                        new Filter {Name = "vpc-id", Values = new List<string>(new[] {workerData.VpcId})}
                    }
                };
                var response = workerData.EC2Client.DescribeSecurityGroups(request);
                securityGroups.AddRange(response.SecurityGroups);
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = new object[] { securityGroups };
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
            {
                var results = e.Result as object[];
                _callback(results[0] as List<SecurityGroup>);
            }
        }

        private QueryVPCSecurityGroupsWorker() { }
    }
}
