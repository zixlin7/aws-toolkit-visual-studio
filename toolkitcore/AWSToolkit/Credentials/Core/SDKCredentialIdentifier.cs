using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Credentials.Core
{
    [DebuggerDisplay("{Id}")]
    public class SDKCredentialIdentifier: ICredentialIdentifier
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string ShortName { get; set; }
        public string FactoryId { get; set; }
        public string ProfileName { get; set; }

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
