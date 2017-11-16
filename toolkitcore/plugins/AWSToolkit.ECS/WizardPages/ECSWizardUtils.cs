using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.CloudWatchEvents;
using Amazon.EC2;
using Amazon.ECS;
using Amazon.ElasticLoadBalancingV2;
using Amazon.IdentityManagement;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;
using Amazon.IdentityManagement.Model;

using log4net;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.ECS.WizardPages
{
    public static class ECSWizardUtils
    {
        internal const string CREATE_NEW_TEXT = "Create New";
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSWizardUtils));


        public static IAmazonECS CreateECSClient(IAWSWizard hostWizard)
        {
            var account = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var client = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(RegionEndPointsManager.ECS_ENDPOINT_LOOKUP));
            return client;
        }

        public static IAmazonElasticLoadBalancingV2 CreateELBv2Client(IAWSWizard hostWizard)
        {
            var account = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var client = account.CreateServiceClient<AmazonElasticLoadBalancingV2Client>(region.GetEndpoint(RegionEndPointsManager.ELB_SERVICE_NAME));
            return client;
        }

        public static IAmazonIdentityManagementService CreateIAMClient(IAWSWizard hostWizard)
        {
            var account = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var client = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME));
            return client;
        }

        public static IAmazonEC2 CreateEC2Client(IAWSWizard hostWizard)
        {
            var account = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var client = account.CreateServiceClient<AmazonEC2Client>(region.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME));
            return client;
        }

        public static IAmazonCloudWatchEvents CreateCloudWatchEventsClient(IAWSWizard hostWizard)
        {
            var account = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var client = account.CreateServiceClient<AmazonCloudWatchEventsClient>(region.GetEndpoint(RegionEndPointsManager.CLOUDWATCH_EVENT_SERVICE_NAME));
            return client;
        }

        public static List<string> LoadECSRoles(IAWSWizard hostWizard)
        {
            using (var client = CreateIAMClient(hostWizard))
            {
                var roles = new List<string>();
                var response = new ListRolesResponse();
                do
                {
                    var request = new ListRolesRequest() { Marker = response.Marker };
                    response = client.ListRoles(request);

                    var validRoles = RolePolicyFilter.FilterByAssumeRoleServicePrincipal(response.Roles, "ecs.amazonaws.com");
                    foreach (var role in validRoles)
                    {
                        roles.Add(role.RoleName);
                    }
                } while (!string.IsNullOrEmpty(response.Marker));
                return roles;
            }
        }

        internal static bool IsFargateLaunch(this IAWSWizard hostWizard)
        {
            var launchType = hostWizard[PublishContainerToAWSWizardProperties.LaunchType] as string;
            return string.Equals(launchType, Amazon.ECS.LaunchType.FARGATE, StringComparison.OrdinalIgnoreCase);
        }


        static IList<TaskCPUItemValue> _taskCPUAllowedValues;

        public static IList<TaskCPUItemValue> TaskCPUAllowedValues
        {
            get
            {
                if(_taskCPUAllowedValues == null)
                {
                    var collection = new List<TaskCPUItemValue>();
                    try
                    {
                        var content = S3FileFetcher.Instance.GetFileContent("ServiceMeta/ECSServiceMeta.json");
                        var rootData = JsonMapper.ToObject(content);
                        var allowedValues = rootData["FargateAllowedCPUSettings"];
                        foreach(JsonData cpuItem in allowedValues)
                        {
                            var c = new TaskCPUItemValue
                            {
                                DisplayName = cpuItem["DisplayName"].ToString(),
                                SystemName = cpuItem["SystemName"].ToString()
                            };

                            c.MemoryOptions = new List<MemoryOption>();
                            foreach(JsonData memoryItem in cpuItem["MemoryValues"])
                            {
                                var m = new MemoryOption
                                {
                                    DisplayName = memoryItem["DisplayName"].ToString(),
                                    SystemName = memoryItem["SystemName"].ToString()
                                };
                                c.MemoryOptions.Add(m);
                            }

                            collection.Add(c);
                        }

                        _taskCPUAllowedValues = collection;

                    }
                    catch(Exception e)
                    {
                        LOGGER.Error("Error loading Fargate allowed CPU values", e);
                    }
                }

                return _taskCPUAllowedValues;
            }
        }


        public class TaskCPUItemValue
        {
            public string DisplayName { get; set; }
            public string SystemName { get; set; }
            public IList<MemoryOption> MemoryOptions { get; set; }
        }

        public class MemoryOption
        {
            public string DisplayName { get; set; }
            public string SystemName { get; set; }
        }


        public class PlacementTemplates
        {
            public static readonly PlacementTemplates[] Options = new PlacementTemplates[]
            {
                new PlacementTemplates
                {
                    DisplayName = "AZ Balanced Spread",
                    Description = "This template will spread tasks across availability zones and within the availability zone spread tasks across instances.",
                    PlacementStrategy = new string[]{ "spread=attribute:ecs.availability-zone", "spread=instanceId" }
                },
                new PlacementTemplates
                {
                    DisplayName = "AZ Balanced BinPack",
                    Description = "This template will spread tasks across availability zones and within the availability zone pack tasks on least number of instances by memory.",
                    PlacementStrategy = new string[]{ "spread=attribute:ecs.availability-zone", "binpack=memory" }
                },
                new PlacementTemplates
                {
                    DisplayName = "BinPack",
                    Description = "This template will pack tasks least number of instances by memory.",
                    PlacementStrategy = new string[]{ "binpack=memory" }
                },
                new PlacementTemplates
                {
                    DisplayName = "One Task Per Host",
                    Description = "This template will place only one task per instance.",
                    PlacementConstraints = new string[]{ "distinctInstance" }
                }
            };

            public string DisplayName { get; set; }
            public string Description { get; set; }

            public string[] PlacementConstraints { get; set; }
            public string[] PlacementStrategy { get; set; }
        }

    }
}
