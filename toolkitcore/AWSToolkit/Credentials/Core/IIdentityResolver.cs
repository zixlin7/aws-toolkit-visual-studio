using System.Threading;
using System.Threading.Tasks;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Encapsulates a way to retrieve identity (like account Ids and Aws Ids)
    /// </summary>
    public interface IIdentityResolver
    {
        Task<string> GetAccountIdAsync(AWSCredentials awsCredentials, RegionEndpoint regionEndpoint, CancellationToken cancellationToken);
        Task<string> GetAwsIdAsync(IAWSTokenProvider tokenProvider, CancellationToken cancellationToken);
    }
}
