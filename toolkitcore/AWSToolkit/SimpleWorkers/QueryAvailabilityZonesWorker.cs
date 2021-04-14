using System;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    /// <summary>
    /// Worker used to do a fetch of the availability zones relevant to a given region
    /// </summary>
    public class QueryAvailabilityZonesWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<AvailabilityZone> data);
        DataAvailableCallback _callback;

        public QueryAvailabilityZonesWorker(IAmazonEC2 ec2Client,
                                            ILog logger,
                                            DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { ec2Client, logger });
        }

        public QueryAvailabilityZonesWorker(AccountViewModel account,
                                            ToolkitRegion region,
                                            ILog logger,
                                            DataAvailableCallback callback)
        {
            _callback = callback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            var ec2Client = account.CreateServiceClient<AmazonEC2Client>(region);

            bw.RunWorkerAsync(new object[] { ec2Client, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            IAmazonEC2 ec2Client = args[0] as IAmazonEC2;
            ILog logger = args[1] as ILog;

            List<AvailabilityZone> zones = new List<AvailabilityZone>();
            try
            {
                DescribeAvailabilityZonesRequest request = new DescribeAvailabilityZonesRequest();
                DescribeAvailabilityZonesResponse response = ec2Client.DescribeAvailabilityZones(request);
                zones.AddRange(response.AvailabilityZones);
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = zones;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<AvailabilityZone>);
        }

        private QueryAvailabilityZonesWorker() { }
    }
}
