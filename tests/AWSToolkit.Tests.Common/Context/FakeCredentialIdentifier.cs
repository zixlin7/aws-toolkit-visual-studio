using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    /// <summary>
    /// An alternate/placeholder ICredentialIdentifier implementation that can
    /// be used by tests.
    /// </summary>
    public class FakeCredentialIdentifier : ICredentialIdentifier
    {
        public string Id { get; set; }
        public string ProfileName { get; set; }
        public string DisplayName { get; set; }
        public string ShortName { get; set; }
        public string FactoryId { get; set; }

        public static FakeCredentialIdentifier Create(string profileName)
        {
            string id = $"fake:{profileName}";
            return new FakeCredentialIdentifier()
            {
                Id = id,
                ProfileName = profileName,
                DisplayName = id,
                ShortName = id,
                FactoryId = "fake",
            };
        }
    }
}
