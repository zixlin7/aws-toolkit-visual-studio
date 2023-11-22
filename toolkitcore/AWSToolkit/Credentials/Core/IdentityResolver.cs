using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Urls;
using Amazon.CodeCatalyst;
using Amazon.CodeCatalyst.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace Amazon.AWSToolkit.Credentials.Core
{
    internal sealed class IdentityResolver : IIdentityResolver
    {
        /// <summary>
        /// Retrieve the AccountId of an AWSCredentials based connection
        /// </summary>
        public async Task<string> GetAccountIdAsync(AWSCredentials awsCredentials, RegionEndpoint regionEndpoint,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stsClient = CreateStsClient(awsCredentials, regionEndpoint);

            var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest(), cancellationToken);
            return response.Account;
        }

        private static AmazonSecurityTokenServiceClient CreateStsClient(AWSCredentials awsCredentials,
            RegionEndpoint regionEndpoint) =>
            ServiceClientCreator.CreateServiceClient(
                typeof(AmazonSecurityTokenServiceClient),
                awsCredentials, regionEndpoint) as AmazonSecurityTokenServiceClient;

        /// <summary>
        /// Retrieve the AWS ID of a token provider based connection.
        /// ASSUMPTION: Token Provider is associated with Sono.
        /// </summary>
        public async Task<string> GetCodeCatalystSessionIdentityAsync(IAWSTokenProvider tokenProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO IDE-12041
            // We are querying from CAWS for the first implementation.
            // Over time, Sono intends to provide a direct means of querying for the user id (AWS ID),
            // and we can switch away from querying CAWS.
            //
            // RISK: if another service comes along that leverages Sono but has their own distinct
            // user id, we'll need to re-design a way of knowing when and how to query the user id from
            // each service. Things will likely converge with Sono. We'll use the simple approach until
            // we need a more complex one, or until we determine that Sono will be the one true source.
            var caws = CreateCodeCatalystClient(tokenProvider);
            var session = await caws.VerifySessionAsync(new VerifySessionRequest(), cancellationToken);

            return session.Identity;
        }

        private static AmazonCodeCatalystClient CreateCodeCatalystClient(IAWSTokenProvider tokenProvider)
        {
            var config = new AmazonCodeCatalystConfig()
            {
                ServiceURL = ServiceUrls.CodeCatalyst,
                AWSTokenProvider = tokenProvider,
            };

            // AmazonCodeCatalystClient has standard service client handling, which tries to resolve AWSCredentials,
            // even though we're using the token provider here. Any system that doesn't have a default credentials profile
            // will get an AmazonServiceException "Unable to get IAM security credentials from EC2 Instance Metadata Service."
            // Pass anonymous credentials to keep the client happy.
            return new AmazonCodeCatalystClient(new AnonymousAWSCredentials(), config);
        }
    }
}
