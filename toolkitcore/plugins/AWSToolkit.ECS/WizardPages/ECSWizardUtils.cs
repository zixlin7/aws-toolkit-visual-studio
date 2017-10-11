using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECS;
using Amazon.ElasticLoadBalancingV2;
using Amazon.IdentityManagement;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.ECS.WizardPages
{
    public static class ECSWizardUtils
    {
        public static IAmazonECS CreateECSClient(IAWSWizard hostWizard)
        {
            var account = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            var client = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(Constants.ECS_ENDPOINT_LOOKUP));
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
    }
}
