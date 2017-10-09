using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECS;
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
    }
}
