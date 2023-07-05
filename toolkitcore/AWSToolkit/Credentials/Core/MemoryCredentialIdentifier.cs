namespace Amazon.AWSToolkit.Credentials.Core
{
    internal class MemoryCredentialIdentifier : CredentialIdentifier
    {
        public MemoryCredentialIdentifier(string profileName)
        {
            Id = $"{MemoryCredentialProviderFactory.MemoryProfileFactoryId}:{profileName}";
            DisplayName = $"Profile:{profileName}";
            ShortName = profileName;
            FactoryId = MemoryCredentialProviderFactory.MemoryProfileFactoryId;
            ProfileName = profileName;
        }
    }
}
