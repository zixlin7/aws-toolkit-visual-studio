using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Util.Internal;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class TokenExtensionMethods
    {
        /// <summary>
        /// Determines if the specified credentials are currently valid.
        /// </summary>
        /// <returns>true if the credentials have a currently valid token, false if the token is not cached, or has expired</returns>
        public static bool HasValidToken(this ICredentialIdentifier credentialsId,
            IAWSToolkitShellProvider toolkitShell)
        {
            return HasValidToken(credentialsId, toolkitShell, null, null);
        }

        /// <summary>
        /// Overload used for testing purposes
        /// </summary>
        public static bool HasValidToken(this ICredentialIdentifier credentialsId,
            IAWSToolkitShellProvider toolkitShell,
            IFile tokenCacheFileProvider,
            IDirectory tokenCacheDirectoryProvider)
        {
            SonoTokenProviderBuilder builder = SonoTokenProviderBuilder.Create()
                .WithToolkitShell(toolkitShell)
                .WithCredentialIdentifier(credentialsId)
                .WithSsoCallback(StubTokenCallback);

            if (tokenCacheFileProvider != null)
            {
                builder = builder.WithTokenCacheFileHandler(tokenCacheFileProvider);
            }

            if (tokenCacheDirectoryProvider != null)
            {
                builder = builder.WithTokenCacheDirectoryHandler(tokenCacheDirectoryProvider);
            }

            var tokenProvider = builder.Build();

            try
            {
                return tokenProvider.TryResolveToken(out AWSToken _);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// This callback ensures that users are not given a prompt to start a SSO login
        /// </summary>
        private static void StubTokenCallback(SsoVerificationArguments credentialIdentifier)
        {
            throw new Exception("SSO Token requires a login flow");
        }
    }
}
