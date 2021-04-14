using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Credentials.Core
{
    [DebuggerDisplay("{Id}")]
    public class SharedCredentialIdentifier : ICredentialIdentifier
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string ShortName { get; set; }
        public string FactoryId { get; set; }
        public string ProfileName { get; set; }

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
