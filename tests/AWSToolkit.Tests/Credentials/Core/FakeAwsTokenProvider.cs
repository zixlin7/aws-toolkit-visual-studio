using System.Threading;
using System.Threading.Tasks;

using Amazon.Runtime;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class FakeAwsTokenProvider : IAWSTokenProvider
    {
        public bool TryResolveToken(out AWSToken token)
        {
            token = CreateToken();
            return true;
        }

        public Task<TryResponse<AWSToken>> TryResolveTokenAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var token = CreateToken();
            return Task.FromResult(new TryResponse<AWSToken>()
            {
                Success = true,
                Value = token,
            });
        }

        private AWSToken CreateToken() => new AWSToken();
    }
}
