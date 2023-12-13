using System;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
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
            SonoTokenProviderBuilder builder)
        {
            return credentialsId.HasValidToken(builder, null, null);
        }

        public static bool HasValidToken(this ICredentialIdentifier credentialsId,
            SonoTokenProviderBuilder builder,
            IFile tokenCacheFileProvider,
            IDirectory tokenCacheDirectoryProvider)
        {
            var builderClone = builder.ShallowClone();

            builderClone.WithSsoCallback(StubTokenCallback);

            if (tokenCacheFileProvider != null)
            {
                builderClone = builderClone.WithTokenCacheFileHandler(tokenCacheFileProvider);
            }

            if (tokenCacheDirectoryProvider != null)
            {
                builderClone = builderClone.WithTokenCacheDirectoryHandler(tokenCacheDirectoryProvider);
            }

            var tokenProvider = builderClone.Build();

            try
            {
                return tokenProvider.TryResolveToken(out _);
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
