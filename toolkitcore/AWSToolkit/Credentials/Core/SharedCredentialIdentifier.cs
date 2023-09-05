namespace Amazon.AWSToolkit.Credentials.Core
{
    public class SharedCredentialIdentifier : CredentialIdentifier
    {
        public SharedCredentialIdentifier(string profileName)
        {
            Id = $"{SharedCredentialProviderFactory.SharedProfileFactoryId}:{profileName}";
            DisplayName = $"Profile:{profileName}";
            ShortName = profileName;
            FactoryId = SharedCredentialProviderFactory.SharedProfileFactoryId;
            ProfileName = profileName;
        }
    }
}
