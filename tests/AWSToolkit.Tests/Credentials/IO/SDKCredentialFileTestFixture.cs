using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Settings;
using Amazon.Util.Internal;

namespace AWSToolkit.Tests.Credentials.IO
{
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
