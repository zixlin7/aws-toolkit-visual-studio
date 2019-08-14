using Amazon.AWSToolkit.Account;
using Amazon.CloudFormation;
using Amazon.AWSToolkit.CloudFormation.Nodes;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages
{
    internal static class DeploymentWizardHelper
    {
        public static IAmazonCloudFormation GetGenericCloudFormationClient(AccountViewModel accountViewModel)
        {
            CloudFormationRootViewModel rootViewModel = accountViewModel.FindSingleChild<CloudFormationRootViewModel>(false);
            if (rootViewModel != null)
            {
                return rootViewModel.CloudFormationClient;
            }
            return new AmazonCloudFormationClient(accountViewModel.Credentials, Amazon.RegionEndpoint.USEast1);
        }
    }
}
