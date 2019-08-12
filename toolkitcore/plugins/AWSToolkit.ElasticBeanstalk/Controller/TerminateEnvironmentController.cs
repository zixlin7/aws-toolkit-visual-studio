using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.ElasticBeanstalk.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class TerminateEnvironmentController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(TerminateEnvironmentController));

        public override ActionResults Execute(IViewModel model)
        {
            EnvironmentViewModel environmentModel = model as EnvironmentViewModel;
            if (environmentModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format(
                "Are you sure you want to terminate the environment \"{0}\"?\r\n\r\n" +
                "Note: By terminating this environment, the running application version " +
                "and the URL http://{1}/ will no longer be available." +
                "It also deletes any Amazon RDS DB Instances created with the environment. To save your data, " +
                "create a snapshot before you terminating your environment."
                , environmentModel.Name, environmentModel.Environment.CNAME);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Terminate Environment", msg))
            {
                var beanstalkClient = environmentModel.BeanstalkClient;

                try
                {
                    LOGGER.DebugFormat("Terminating environment {0}", environmentModel.Environment.EnvironmentId);
                    beanstalkClient.TerminateEnvironment(new TerminateEnvironmentRequest() { EnvironmentId = environmentModel.Environment.EnvironmentId });

                    ThreadPool.QueueUserWorkItem(this.TryDeleteRDSSecurityGroup, environmentModel);
                }
                catch (Exception e)
                {
                    LOGGER.Error(string.Format("Error terminating environment {0}", environmentModel.Environment.EnvironmentId), e);
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Error Terminating", "Error terminating environment: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }

            return new ActionResults().WithSuccess(true);
        }

        public void TryDeleteRDSSecurityGroup(object state)
        {
            EnvironmentViewModel environmentModel = state as EnvironmentViewModel;
            try
            {
                var endpoints = RegionEndPointsManager.GetInstance().GetRegion(environmentModel.ApplicationViewModel.RegionSystemName);
                var ec2Client = environmentModel.AccountViewModel.CreateServiceClient<AmazonEC2Client>(endpoints);
                var securityGroupName = environmentModel.Name + Amazon.AWSToolkit.Constants.BEANSTALK_RDS_SECURITY_GROUP_POSTFIX;

                DescribeSecurityGroupsResponse response = null;
                try
                {
                    response = ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest()
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
                var rdsClient = environmentModel.AccountViewModel.CreateServiceClient<AmazonRDSClient>(endpoints);
                foreach (var dbSecurityGroup in rdsClient.DescribeDBSecurityGroups().DBSecurityGroups)
                {
                    foreach (var ec2SecurityGroup in dbSecurityGroup.EC2SecurityGroups)
                    {
                        if (string.Equals(ec2SecurityGroup.EC2SecurityGroupName, securityGroupName))
                        {
                            rdsClient.RevokeDBSecurityGroupIngress(new RevokeDBSecurityGroupIngressRequest()
                            {
                                DBSecurityGroupName = dbSecurityGroup.DBSecurityGroupName,
                                EC2SecurityGroupId = ec2SecurityGroup.EC2SecurityGroupId,
                                EC2SecurityGroupOwnerId = ec2SecurityGroup.EC2SecurityGroupOwnerId
                            });
                        }
                    }
                }

                // Wait for environment to be terminated so that all instance will be gone and the group can be deleted.
                var describeRequest = new DescribeEnvironmentsRequest(){EnvironmentNames = new List<string>(){environmentModel.Name}};
                var beanstalkClient = environmentModel.BeanstalkClient;

                long start = DateTime.Now.Ticks;
                while (new TimeSpan(DateTime.Now.Ticks - start).TotalMinutes < 5)
                {
                    Thread.Sleep(20 * 1000);
                    var describeResponse = beanstalkClient.DescribeEnvironments(describeRequest);
                    if (describeResponse.Environments.Count != 1 || describeResponse.Environments[0].Status == BeanstalkConstants.STATUS_TERMINATED)
                        break;
                }

                ec2Client.DeleteSecurityGroup(new DeleteSecurityGroupRequest()
                {
                    GroupId = createdEC2SecurityGroup.GroupId
                });
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Failed to deleted security group created for allowing RDS access: " + e.Message);
            }
        }
    }
}
