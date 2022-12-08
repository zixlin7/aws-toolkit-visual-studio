using System;
using System.Collections.Generic;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.Navigator;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class TerminateEnvironmentController : BaseConnectionContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(TerminateEnvironmentController));

        private readonly BeanstalkEnvironmentModel _beanstalkEnvironment;

        private readonly AmazonElasticBeanstalkClient _beanstalk;
        private readonly AmazonEC2Client _ec2;
        private readonly AmazonRDSClient _rds;

        public TerminateEnvironmentController(BeanstalkEnvironmentModel beanstalkEnvironment,
            ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
            : base(toolkitContext, connectionSettings)
        {
            _beanstalkEnvironment = beanstalkEnvironment;

            _beanstalk = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticBeanstalkClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _ec2 = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _rds = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonRDSClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
        }

        public override ActionResults Execute()
        {
            if (_beanstalkEnvironment == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            string msg = string.Format(
                "Are you sure you want to terminate the environment \"{0}\"?\r\n\r\n" +
                "Note: By terminating this environment, the running application version " +
                "and the URL http://{1}/ will no longer be available." +
                "It also deletes any Amazon RDS DB Instances created with the environment. To save your data, " +
                "create a snapshot before you terminating your environment."
                , _beanstalkEnvironment.Name, _beanstalkEnvironment.Cname);
            if (_toolkitContext.ToolkitHost.Confirm("Terminate Environment", msg))
            {
                try
                {
                    _logger.DebugFormat("Terminating environment {0}", _beanstalkEnvironment.Id);
                    _beanstalk.TerminateEnvironment(new TerminateEnvironmentRequest() { EnvironmentId = _beanstalkEnvironment.Id });

                    ThreadPool.QueueUserWorkItem(this.TryDeleteRDSSecurityGroup, null);
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("Error terminating environment {0}", _beanstalkEnvironment.Id), e);
                    _toolkitContext.ToolkitHost.ShowMessage("Error Terminating", "Error terminating environment: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }

            return new ActionResults().WithSuccess(true);
        }

        private void TryDeleteRDSSecurityGroup(object state)
        {
            try
            {
                var securityGroupName = _beanstalkEnvironment.Name + Amazon.AWSToolkit.Constants.BEANSTALK_RDS_SECURITY_GROUP_POSTFIX;

                DescribeSecurityGroupsResponse response = null;
                try
                {
                    response = _ec2.DescribeSecurityGroups(new DescribeSecurityGroupsRequest()
                    {
                        GroupNames = new List<string>() { securityGroupName }
                    });
                }
                catch
                {
                    // Group does not exist
                }

                // No group found so abort.
                if (response == null || response.SecurityGroups.Count != 1)
                    return;

                var createdEC2SecurityGroup = response.SecurityGroups[0];

                // Delete all the RDS associations before polling on the terminate to make sure there is no eventual consistence issues when finally deleting the group.
                foreach (var dbSecurityGroup in _rds.DescribeDBSecurityGroups().DBSecurityGroups)
                {
                    foreach (var ec2SecurityGroup in dbSecurityGroup.EC2SecurityGroups)
                    {
                        if (string.Equals(ec2SecurityGroup.EC2SecurityGroupName, securityGroupName))
                        {
                            _rds.RevokeDBSecurityGroupIngress(new RevokeDBSecurityGroupIngressRequest()
                            {
                                DBSecurityGroupName = dbSecurityGroup.DBSecurityGroupName,
                                EC2SecurityGroupId = ec2SecurityGroup.EC2SecurityGroupId,
                                EC2SecurityGroupOwnerId = ec2SecurityGroup.EC2SecurityGroupOwnerId
                            });
                        }
                    }
                }

                // Wait for environment to be terminated so that all instance will be gone and the group can be deleted.
                var describeRequest = new DescribeEnvironmentsRequest(){EnvironmentNames = new List<string>(){ _beanstalkEnvironment.Name}};

                long start = DateTime.Now.Ticks;
                while (new TimeSpan(DateTime.Now.Ticks - start).TotalMinutes < 5)
                {
                    Thread.Sleep(20 * 1000);
                    var describeResponse = _beanstalk.DescribeEnvironments(describeRequest);
                    if (describeResponse.Environments.Count != 1 || describeResponse.Environments[0].Status == BeanstalkConstants.STATUS_TERMINATED)
                        break;
                }

                _ec2.DeleteSecurityGroup(new DeleteSecurityGroupRequest()
                {
                    GroupId = createdEC2SecurityGroup.GroupId
                });
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole("Failed to deleted security group created for allowing RDS access: " + e.Message);
            }
        }
    }
}
