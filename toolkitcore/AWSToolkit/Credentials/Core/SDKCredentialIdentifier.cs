namespace Amazon.AWSToolkit.Credentials.Core
{
    public class SDKCredentialIdentifier : CredentialIdentifier
    {
        public SDKCredentialIdentifier(string profileName)
        {
            Id = $"{SDKCredentialProviderFactory.SdkProfileFactoryId}:{profileName}";
            DisplayName = $"sdk:{profileName}";
            ShortName = profileName;
            FactoryId = SDKCredentialProviderFactory.SdkProfileFactoryId;
            ProfileName = profileName;
        }
    }
}
