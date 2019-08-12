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
    public class QueryVPCPropertiesWorker
    {
        public delegate void DataAvailableCallback(VPCPropertyData data);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonEC2 EC2Client { get; set; }
            public string VPCId { get; set; }
            public ILog Logger { get; set; }
        }

        public class VPCPropertyData
        {
            public string DefaultSecurityGroupId { get; set; }
            public IEnumerable<KeyValuePair<string, Subnet>> Subnets { get; set; }
            public IEnumerable<KeyValuePair<string, SecurityGroup>> SecurityGroups { get; set; }
        }

        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        public static VPCPropertyData QueryVPCProperties(AccountViewModel accountViewModel,
                                                         RegionEndPointsManager.RegionEndPoints region,
                                                         string vpcId,
                                                         ILog logger)
        {
            var workerData = new WorkerData
            {
                EC2Client = DeploymentWizardHelper.GetEC2Client(accountViewModel, region),
                VPCId = vpcId,
                Logger = logger
            };

            var vpcProperties = new VPCPropertyData();
            new QueryVPCPropertiesWorker().RunQuery(workerData, vpcProperties);

            return vpcProperties;
        }

        public QueryVPCPropertiesWorker(AccountViewModel accountViewModel,
                                                RegionEndPointsManager.RegionEndPoints region,
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
                                    EC2Client = DeploymentWizardHelper.GetEC2Client(accountViewModel, region),
                                    VPCId  = vpcId,
                                    Logger = logger
                                  });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;
            var data = new VPCPropertyData();
            RunQuery(workerData, data);
            e.Result = data;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as VPCPropertyData);
        }

        // shared by async and synchronous worker versions
        void RunQuery(WorkerData workerData, VPCPropertyData vpcPropertyData)
        {
            workerData.Logger.InfoFormat("QueryVPCProperties: requesting properties for VPC ID {0}", workerData.VPCId);
            try
            {
                var filters = new List<Filter> { new Filter() { Name = "vpc-id", Values = new List<string> { workerData.VPCId } } };

                var subnetResponse = workerData.EC2Client.DescribeSubnets(new DescribeSubnetsRequest() { Filters = filters });
                var subnets = new List<KeyValuePair<string, Subnet>>();

                foreach (var item in subnetResponse.Subnets)
                {
                    string key;
                    var tag = item.Tags.FirstOrDefault(x => x.Key == "Name");
                    if (tag != null)
                        key = string.Format("{0} - {1} ({2} - {3})", tag.Value, item.SubnetId, item.CidrBlock, item.AvailabilityZone);
                    else
                        key = string.Format("{0} ({1} - {2})", item.SubnetId, item.CidrBlock, item.AvailabilityZone);

                    subnets.Add(new KeyValuePair<string, Subnet>(key, item));
                }
                vpcPropertyData.Subnets = subnets;


                var securityGroupsResponse = workerData.EC2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest() { Filters = filters });
                var groups = new List<KeyValuePair<string, SecurityGroup>>();

                foreach (var item in securityGroupsResponse.SecurityGroups)
                {
                    if (item.GroupName.Equals("default", StringComparison.OrdinalIgnoreCase))
                        vpcPropertyData.DefaultSecurityGroupId = item.GroupId;

                    string key;
                    var tag = item.Tags.FirstOrDefault(x => x.Key == "Name");
                    if (tag != null)
                        key = string.Format("{0} - {1} ({2})", tag.Value, item.GroupName, item.GroupId);
                    else
                        key = string.Format("{0} ({1})", item.GroupName, item.GroupId);

                    groups.Add(new KeyValuePair<string, SecurityGroup>(key, item));
                }
                vpcPropertyData.SecurityGroups = groups;
            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception: ", exc);
            }
        }

        private QueryVPCPropertiesWorker() { }
    }
}
