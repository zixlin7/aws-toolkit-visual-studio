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

namespace Amazon.AWSToolkit.ECS.WizardPages
{
    public static class ECSWizardUtils
    {
        internal const string CREATE_NEW_TEXT = "Create New";


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

        public class TaskCPUItemValue
        {
            public static readonly TaskCPUItemValue VCPU_0_25 = new TaskCPUItemValue
            {
                DisplayName = "0.25 vCPU (256)",
                SystemName = "256",
                MemoryOptions = new string[] { "512MB", "1GB", "2GB" }
            };
            public static readonly TaskCPUItemValue VCPU_0_50 = new TaskCPUItemValue
            {
                DisplayName = "0.50 vCPU (512)",
                SystemName = "512",
                MemoryOptions = GetOptions(1, 4)
            };
            public static readonly TaskCPUItemValue VCPU_1_00 = new TaskCPUItemValue
            {
                DisplayName = "1.00 vCPU (1024)",
                SystemName = "1024",
                MemoryOptions = GetOptions(2, 8)
            };
            public static readonly TaskCPUItemValue VCPU_2_00 = new TaskCPUItemValue
            {
                DisplayName = "2.00 vCPU (2048)",
                SystemName = "2048",
                MemoryOptions = GetOptions(4, 16)
            };
            public static readonly TaskCPUItemValue VCPU_4_00 = new TaskCPUItemValue
            {
                DisplayName = "4.00 vCPU (4096)",
                SystemName = "4096",
                MemoryOptions = GetOptions(8, 30)
            };

            public static readonly IEnumerable<TaskCPUItemValue> AllValues = new TaskCPUItemValue[]
            {
                VCPU_0_25,
                VCPU_0_50,
                VCPU_1_00,
                VCPU_2_00,
                VCPU_4_00
            };

            private static string [] GetOptions(int min, int max)
            {
                var values = new List<string>();
                for(int i = min; i <= max; i++)
                {
                    values.Add(i + "GB");
                }

                return values.ToArray();
            }


            public string DisplayName { get; set; }
            public string SystemName { get; set; }
            public string[] MemoryOptions { get; set; }
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
