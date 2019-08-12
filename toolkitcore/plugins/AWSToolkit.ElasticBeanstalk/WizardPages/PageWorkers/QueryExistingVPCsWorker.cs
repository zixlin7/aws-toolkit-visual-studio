using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.Account;

using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;
namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    public class QueryExistingVPCsWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<KeyValuePair<string, Vpc>> data);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryExistingVPCsWorker(AccountViewModel accountViewModel,
                                                RegionEndPointsManager.RegionEndPoints region,
                                                ILog logger,
                                                DataAvailableCallback callback)
        {
            _callback = callback;

            var bw = new BackgroundWorker();
            bw.DoWork += Worker;
            bw.RunWorkerCompleted += WorkerCompleted;
            bw.RunWorkerAsync(new WorkerData
                                  { 
                                    EC2Client = DeploymentWizardHelper.GetEC2Client(accountViewModel, region),
                                    Logger = logger
                                  });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var vpcs = new List<KeyValuePair<string, Vpc>>();
            try
            {
                var response = workerData.EC2Client.DescribeVpcs(new DescribeVpcsRequest());

                foreach (var vpc in response.Vpcs)
                {
                    string key;
                    if (vpc.IsDefault)
                        key = string.Format("Default VPC ({0})", vpc.VpcId);
                    else    
                    {
                        var tag = vpc.Tags.FirstOrDefault(x => x.Key == "Name");
                        key = tag != null
                            ? string.Format("{0} - {1} ({2})", tag.Value, vpc.VpcId, vpc.CidrBlock)
                            : string.Format("{0} ({1})", vpc.VpcId, vpc.CidrBlock);
                    }

                    vpcs.Add(new KeyValuePair<string, Vpc>(key, vpc));
                }
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = vpcs;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<KeyValuePair<string, Vpc>>);
        }

        private QueryExistingVPCsWorker() { }
    }
}
