using System.Diagnostics;

namespace Amazon.AWSToolkit.Credentials.Core
{
    [DebuggerDisplay("{Id}")]
    internal class MemoryCredentialIdentifier : ICredentialIdentifier
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public string ShortName { get; set; }

        public string FactoryId { get; set; }

        public string ProfileName { get; set; }

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
