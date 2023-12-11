using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public static class ICredentialIdentifierExtensionMethods
    {
        public static bool IsBuilderId(this ICredentialIdentifier credentialIdentifier)
        {
            return credentialIdentifier.FactoryId.Equals(SonoCredentialProviderFactory.FactoryId);
        }

        public static bool HasValidCodeWhispererConnection(this ICredentialIdentifier credentialIdentifier, ToolkitContext toolkitContext)
        {
            if (credentialIdentifier == null)
            {
                return false;
            }

            var profileProps = toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);

            if (profileProps == null)
            {
                return false;
            }

            var builder = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(credentialIdentifier)
                .WithSessionName(profileProps.SsoSession)
                .WithToolkitShell(toolkitContext.ToolkitHost);

            if (!credentialIdentifier.IsBuilderId())
            {
                var ssoRegion = RegionEndpoint.GetBySystemName(profileProps.SsoRegion);

                builder.WithIsBuilderId(false)
                    .WithOidcRegion(ssoRegion)
                    .WithStartUrl(profileProps.SsoStartUrl);
            }

            return credentialIdentifier.HasValidToken(builder);
        }
    }
}
