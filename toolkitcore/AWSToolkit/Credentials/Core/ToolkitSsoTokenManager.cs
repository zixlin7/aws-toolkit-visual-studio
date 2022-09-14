using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.Runtime.Credentials.Internal;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Wraps a provided SSO Token Manager in order to configure the Token Request options as needed.
    /// </summary>
    public sealed class ToolkitSsoTokenManager : ISSOTokenManager
    {
        private readonly ISSOTokenManager _ssoTokenManager;
        private readonly ToolkitSsoTokenManagerOptions _toolkitTokenManagerOptions;

        public ToolkitSsoTokenManager(ISSOTokenManager ssoTokenManager, ToolkitSsoTokenManagerOptions toolkitTokenManagerOptions)
        {
            _ssoTokenManager = ssoTokenManager;
            _toolkitTokenManagerOptions = toolkitTokenManagerOptions;
        }

        public SsoToken GetToken(SSOTokenManagerGetTokenOptions options)
        {
            SetupOptions(options);

            return _ssoTokenManager.GetToken(options);
        }

        public Task<SsoToken> GetTokenAsync(SSOTokenManagerGetTokenOptions options, CancellationToken cancellationToken = default)
        {
            SetupOptions(options);

            return _ssoTokenManager.GetTokenAsync(options, cancellationToken);
        }

        private void SetupOptions(SSOTokenManagerGetTokenOptions options)
        {
            options.SsoVerificationCallback = _toolkitTokenManagerOptions.SsoVerificationCallback;
            options.SupportsGettingNewToken = true;
            options.ClientName = _toolkitTokenManagerOptions.ClientName;
            options.ClientType = _toolkitTokenManagerOptions.ClientType;

            if (options.Scopes == null)
            {
                options.Scopes = new List<string>();
            }

            options.Scopes.AddAll(_toolkitTokenManagerOptions.Scopes.Except(options.Scopes));
        }
    }
}
