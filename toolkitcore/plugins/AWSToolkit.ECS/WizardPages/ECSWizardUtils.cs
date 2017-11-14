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
