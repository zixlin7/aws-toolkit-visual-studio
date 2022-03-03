using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Credentials.Presentation
{
    public static class CredentialIdentifierExtensionMethods
    {
        public static CredentialsIdentifierGroup GetPresentationGroup(this ICredentialIdentifier credentialIdentifier)
        {
            switch (credentialIdentifier?.FactoryId)
            {
                case SharedCredentialProviderFactory.SharedProfileFactoryId:
                    return CredentialsIdentifierGroup.SharedCredentials;
                case SDKCredentialProviderFactory.SdkProfileFactoryId:
                    return CredentialsIdentifierGroup.SdkCredentials;
                default:
                    // Fall back to a "catch-all" group
                    return CredentialsIdentifierGroup.AdditionalCredentials;
            }
        }
    }
}
