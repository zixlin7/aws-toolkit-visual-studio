using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    /// <summary>
    /// Simple background worker wrapper to retrieve a set of security groups defined
    /// by a particular account
    /// </summary>
    public class QuerySecurityGroupsWorker
    {
        public delegate void DataAvailableCallback(ICollection<SecurityGroup> data);
        public delegate void ErrorCallback(Exception e);
        DataAvailableCallback _callback;
        ErrorCallback _errorCallback;

        /// <summary>
        /// Perform synchronous fetch 
        /// </summary>
        /// <param name="ec2Client"></param>
        public static List<SecurityGroup> FetchGroups(IAmazonEC2 ec2Client, string vpcId, ILog logger)
        {
            List<SecurityGroup> images = new List<SecurityGroup>();

            QuerySecurityGroupsWorker workerObj = new QuerySecurityGroupsWorker();
            workerObj.QueryGroups(ec2Client, vpcId, logger, images);
            return images;
        }

        public QuerySecurityGroupsWorker(IAmazonEC2 ec2Client,
                                         string vpcId,
                                         ILog logger,
                                         DataAvailableCallback callback)
            : this(ec2Client, vpcId, logger, callback, null)
        {

        }

        public QuerySecurityGroupsWorker(IAmazonEC2 ec2Client,
                                         string vpcId,
                                         ILog logger,
                                         DataAvailableCallback callback,
                                         ErrorCallback errorCallback)
        {
            _callback = callback;
            _errorCallback = errorCallback;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += new DoWorkEventHandler(Worker);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
            bw.RunWorkerAsync(new object[] { ec2Client, vpcId, logger });
        }

        void Worker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            IAmazonEC2 ec2Client = args[0] as IAmazonEC2;
            string vpcId = args[1] as string;
            ILog logger = args[2] as ILog;

            List<SecurityGroup> groups = new List<SecurityGroup>();
            QueryGroups(ec2Client, vpcId, logger, groups);
            e.Result = groups;
        }

        void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_callback != null)
                _callback(e.Result as List<SecurityGroup>);
        }

        void QueryGroups(IAmazonEC2 ec2Client, string vpcId, ILog logger, List<SecurityGroup> groups)
        {
            try
            {
                if (vpcId == null)
                {
                    var accountAttributes = ec2Client.DescribeAccountAttributes(new DescribeAccountAttributesRequest()).AccountAttributes;
                    if (accountAttributes != null)
                    {
                        // Test if VPC by Default only account, if it is then we need the security group for the VPC.
                        var platforms = accountAttributes.FirstOrDefault(x => string.Equals(x.AttributeName, "supported-platforms", StringComparison.OrdinalIgnoreCase));
                        if (platforms.AttributeValues.Count == 1 && platforms.AttributeValues[0].AttributeValue == "VPC")
                        {
                            var vpcAttribute = accountAttributes.FirstOrDefault(x => string.Equals(x.AttributeName, "default-vpc", StringComparison.OrdinalIgnoreCase));
                            vpcId = vpcAttribute.AttributeValues[0].AttributeValue;
                        }
                    }
                }

                DescribeSecurityGroupsResponse response = ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest());

                foreach (var group in response.SecurityGroups)
                {
                    if (string.IsNullOrEmpty(vpcId))
                    {
                        if (string.IsNullOrEmpty(group.VpcId))
                            groups.Add(group);
                    }
                    else if (vpcId.Equals(group.VpcId))
                    {
                        groups.Add(group);
                    }
                }
            }
            catch (Exception exc)
            {
                if(this._errorCallback != null)
                {
                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                    {
                        this._errorCallback(exc);
                    }));
                }
                logger.Error(GetType().FullName + ", exception in QueryGroups", exc);
            }
        }

        private QuerySecurityGroupsWorker() { }
    }

}
