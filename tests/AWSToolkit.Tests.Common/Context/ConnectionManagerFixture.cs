using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

using Moq;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class ConnectionManagerFixture
    {
        public static IAwsConnectionManager Create()
        {
            var connectionManager = new Mock<IAwsConnectionManager>();

            connectionManager.SetupGet(m => m.ActiveAccountId).Returns("12345");
            connectionManager.SetupGet(m => m.ActiveCredentialIdentifier).Returns(CreateCredentialIdentifier());
            connectionManager.SetupGet(m => m.ActiveRegion).Returns(CreateUsWestRegion());

            return connectionManager.Object;
        }

        private static ICredentialIdentifier CreateCredentialIdentifier()
        {
            return FakeCredentialIdentifier.Create("default");
        }

        private static ToolkitRegion CreateUsWestRegion()
        {
            return new ToolkitRegion { Id = "us-west-2", DisplayName = "US West (Oregon)", PartitionId = "aws" };
        }

        public static AwsConnectionSettings CreateAwsConnectionSettings()
        {
            return new AwsConnectionSettings(CreateCredentialIdentifier(), CreateUsWestRegion());
        }
    }
}
