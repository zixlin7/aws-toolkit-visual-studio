using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class FakeTokenProvider : IAWSTokenProvider
    {
        public string Token { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public FakeTokenProvider(string token = "FakeToken", DateTime? expiresAt = null)
        {
            Token = token;
            ExpiresAt = expiresAt;
        }

        public bool TryResolveToken(out AWSToken token)
        {
            token = new AWSToken() { Token = Token };
            if (ExpiresAt != null)
            {
                token.ExpiresAt = ExpiresAt;
            }

            return true;
        }

        public Task<TryResponse<AWSToken>> TryResolveTokenAsync(CancellationToken cancellationToken = default)
        {
            TryResolveToken(out AWSToken token);
            return Task.FromResult(new TryResponse<AWSToken>() { Success = true, Value = token });
        }
    }
}
