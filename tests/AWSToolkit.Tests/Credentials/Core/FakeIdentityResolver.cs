using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.Runtime;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class FakeIdentityResolver : IIdentityResolver
    {
        public string AccountId { get; set; }
        public bool GetAccountIdAsyncThrows { get; set; }
        public string AwsId { get; set; }
        public bool GetAwsIdAsyncThrows { get; set; }

        public Task<string> GetAccountIdAsync(AWSCredentials awsCredentials, RegionEndpoint regionEndpoint,
            CancellationToken cancellationToken)
        {
            if (GetAccountIdAsyncThrows)
            {
                throw new Exception("Simulate failure");
            }

            return Task.FromResult(AccountId);
        }

        public Task<string> GetCodeCatalystSessionIdentityAsync(IAWSTokenProvider tokenProvider, CancellationToken cancellationToken)
        {
            if (GetAwsIdAsyncThrows)
            {
                throw new Exception("Simulate failure");
            }

            return Task.FromResult(AwsId);
        }
    }
}
