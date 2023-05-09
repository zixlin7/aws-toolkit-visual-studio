using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.Runtime;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class IdentityResolverTests
    {
        private readonly IdentityResolver _sut = new IdentityResolver();

        [Fact]
        public async Task GetAwsIdAsync_DoesNotThrowServiceException()
        {
            // This test guards against AmazonCodeCatalystClient raising a AmazonServiceException in the event that the service client attempts
            // to resolve IAM based AWSCredentials. If that were to happen, an "Unable to get IAM security credentials from EC2 Instance Metadata Service."
            // exception is raised.
            // We know this client should be using a bearer token. We're passing it a fake token provider, which will return a
            // "No Token found.  Operation requires a Bearer token." AmazonClientException
            await Assert.ThrowsAsync<AmazonClientException>(() => _sut.GetAwsIdAsync(new FakeAwsTokenProvider(), CancellationToken.None));
        }
    }
}
