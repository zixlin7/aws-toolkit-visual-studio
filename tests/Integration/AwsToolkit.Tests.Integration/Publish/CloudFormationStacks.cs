using System.Threading.Tasks;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class CloudFormationStacks
    {
        public static async Task DeleteStack(string stackName)
        {
            var cfnClient = new AmazonCloudFormationClient();
            var request = new DeleteStackRequest
            {
                StackName = stackName
            };
            await cfnClient.DeleteStackAsync(request);
        }
    }
}
