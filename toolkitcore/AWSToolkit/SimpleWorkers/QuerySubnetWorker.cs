using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Account;

using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class QuerySubnetWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<Subnet> data);
        DataAvailableCallback _callback;

        public QuerySubnetWorker(IAmazonEC2 ec2Client,
                                            ILog logger,
                                            DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { ec2Client, logger });
        }

        public QuerySubnetWorker(AccountViewModel account,
                                            RegionEndPointsManager.RegionEndPoints regionEndPoints,
                                            ILog logger,
                                            DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);

            var region = regionEndPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);
            var ec2Config = new AmazonEC2Config();
            region.ApplyToClientConfig(ec2Config);

            IAmazonEC2 ec2Client = new AmazonEC2Client(account.Credentials, ec2Config);

            bw.RunWorkerAsync(new object[] { ec2Client, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            IAmazonEC2 ec2Client = args[0] as IAmazonEC2;
            ILog logger = args[1] as ILog;

            List<Subnet> subnets = new List<Subnet>();
            try
            {
                DescribeSubnetsRequest request = new DescribeSubnetsRequest();
                var response = ec2Client.DescribeSubnets(request);
                subnets.AddRange(response.Subnets);
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = subnets;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<Subnet>);
        }

        private QuerySubnetWorker() { }
    }
}
