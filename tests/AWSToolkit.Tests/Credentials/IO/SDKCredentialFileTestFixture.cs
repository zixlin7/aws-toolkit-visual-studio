using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Settings;
using Amazon.Util.Internal;
using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    [CollectionDefinition(SdkCredentialCollectionDefinition.NonParallelTests, DisableParallelization = true)]
    public class SdkCredentialCollectionDefinition
    {
        public const string NonParallelTests = "Non-parallel Credentials tests";
    }

    public class SDKCredentialFileTestFixture : EncryptedStoreTestFixture
    {
        private const string ProfilesFilename = SettingsConstants.RegisteredProfiles + ".json";

        public NetSDKCredentialsFile ProfileStore { get; set; }
        public NamedSettingsManager Manager { get;  set; }

        public SDKCredentialFileTestFixture() : this(null)
        {
        }

        public SDKCredentialFileTestFixture(string fileContents)
            : base(ProfilesFilename, fileContents)
        {
            ProfileStore = new NetSDKCredentialsFile();
            Manager = new NamedSettingsManager(SettingsConstants.RegisteredProfiles);
        }
    }
}
