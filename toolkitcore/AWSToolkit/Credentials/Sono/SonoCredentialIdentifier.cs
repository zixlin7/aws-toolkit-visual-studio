using System.Diagnostics;

using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Credentials.Sono
{
    /// <summary>
    /// Represents credentials capable of connecting to Sono
    /// </summary>
    [DebuggerDisplay("{Id}")]
    public class SonoCredentialIdentifier : ICredentialIdentifier
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string ShortName { get; set; }
        public string FactoryId { get; set; }
        public string ProfileName { get; set; }

        public SonoCredentialIdentifier(string profileName)
        {
            var factoryId = SonoCredentialProviderFactory.FactoryId;

            Id = $"{factoryId}:{profileName}";
            DisplayName = $"awsBuilderId:{profileName}";
            ShortName = profileName;
            FactoryId = factoryId;
            ProfileName = profileName;
        }
    }
}
