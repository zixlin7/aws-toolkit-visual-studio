using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers
{
    /// <summary>
    /// Worker to determine the user's set of RDS security groups and instances
    /// that are using them.
    /// </summary>
    internal class QueryRDSGroupsAndInstancesWorker
    {
        public delegate void DataAvailableCallback(IEnumerable<SelectableGroup<SecurityGroupInfo>> data);

        readonly DataAvailableCallback _callback;

        struct WorkerData
        {
            public IAmazonRDS RDSClient { get; set; }
            public ILog Logger { get; set; }
        }

        public QueryRDSGroupsAndInstancesWorker(AccountViewModel accountViewModel, 
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
                RDSClient = DeploymentWizardHelper.GetRDSClient(accountViewModel, region),
                Logger = logger
            });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            var workerData = (WorkerData)e.Argument;

            var data = new List<SelectableGroup<SecurityGroupInfo>>();
            try
            {
                var instancesResponse = workerData.RDSClient.DescribeDBInstances(new DescribeDBInstancesRequest());
                var dbInstances = instancesResponse.DBInstances;

                var groupsResponse = workerData.RDSClient.DescribeDBSecurityGroups();
                var dbSecurityGroups = groupsResponse.DBSecurityGroups;

                foreach (var group in dbSecurityGroups)
                {
                    var sb = new StringBuilder();
                    foreach (var dbInstance in dbInstances)
                    {
                        if (!dbInstance.DBInstanceStatus.Equals("available", StringComparison.OrdinalIgnoreCase))
                            continue;

                        foreach (var groupMembership in dbInstance.DBSecurityGroups)
                        {
                            if (string.Equals(groupMembership.DBSecurityGroupName, group.DBSecurityGroupName, StringComparison.Ordinal))
                            {
                                if (sb.Length > 0)
                                    sb.AppendFormat(",{0}", dbInstance.DBInstanceIdentifier);
                                else
                                    sb.Append(dbInstance.DBInstanceIdentifier);
                            }
                        }
                    }

                    // if no referencing instances, don't list it
                    if (sb.Length > 0)
                        data.Add(new SelectableGroup<SecurityGroupInfo>(new SecurityGroupInfo{ Name=group.DBSecurityGroupName }, sb.ToString()));
                }

                // need to invert ordering for vpc groups so we get vpcgroup:instance-membership, as for db groups
                var vpcGroupHash = new Dictionary<string, List<string>>();
                foreach (var dbInstance in dbInstances)
                {
                    if (!dbInstance.VpcSecurityGroups.Any())
                        continue;

                    if (!dbInstance.DBInstanceStatus.Equals("available", StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var vpcsg in dbInstance.VpcSecurityGroups)
                    {
                        List<string> instanceMembers;
                        if (vpcGroupHash.ContainsKey(vpcsg.VpcSecurityGroupId))
                            instanceMembers = vpcGroupHash[vpcsg.VpcSecurityGroupId];
                        else
                        {
                            instanceMembers = new List<string>();
                            vpcGroupHash.Add(vpcsg.VpcSecurityGroupId, instanceMembers);
                        }
                        instanceMembers.Add(string.Format("{0} (port {1})", dbInstance.DBInstanceIdentifier, dbInstance.Endpoint.Port));
                    }
                }

                foreach (var vpcGroupId in vpcGroupHash.Keys)
                {
                    var instanceMembers = vpcGroupHash[vpcGroupId];
                    var sb = new StringBuilder();
                    foreach (var im in instanceMembers)
                    {
                        if (sb.Length > 0)
                            sb.Append(",");
                        sb.Append(im);
                    }

                    data.Add(new SelectableGroup<SecurityGroupInfo>(new SecurityGroupInfo { Id = vpcGroupId }, sb.ToString()));
                }

            }
            catch (Exception exc)
            {
                workerData.Logger.Error(GetType().FullName + ", exception in Worker", exc);
            }

            e.Result = data;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as IEnumerable<SelectableGroup<SecurityGroupInfo>>);
        }

        private QueryRDSGroupsAndInstancesWorker() { }
    }
}
