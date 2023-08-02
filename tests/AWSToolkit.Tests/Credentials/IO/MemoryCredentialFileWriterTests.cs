using System.Linq;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class MemoryCredentialFileWriterTests
    {
        private readonly MemoryCredentialFileWriter _sut;

        private readonly MemoryCredentialFileReader _fileReader;

        private readonly MemoryCredentialsFile _file;

        private readonly CredentialProfile _sampleStaticProfile = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret;

        private readonly CredentialProfile _sampleAlternateStaticProfile = CredentialProfileTestHelper.Basic.Valid.Token;

        private readonly CredentialProfile _sampleSsoWithSsoSessionProfile = CredentialProfileTestHelper.SsoWithSsoSession.ValidProfile;

        public MemoryCredentialFileWriterTests()
        {
            _file = new MemoryCredentialsFile();
            _sut = new MemoryCredentialFileWriter(_file);
            _fileReader = new MemoryCredentialFileReader(_file);
            _fileReader.Load();
        }

        [Fact]
        public void CreateProfileTestWithStaticProfile()
        {
            Assert.Empty(_fileReader.ProfileNames);
            _sut.CreateOrUpdateProfile(_sampleStaticProfile);
            Assert.Equal(_sampleStaticProfile, _fileReader.GetCredentialProfile(_sampleStaticProfile.Name));
        }

        [Fact]
        public void CreateProfileTestWithSsoSession()
        {
            Assert.Empty(_fileReader.ProfileNames);
            _sut.CreateOrUpdateProfile(_sampleSsoWithSsoSessionProfile);
            Assert.Equal(_sampleSsoWithSsoSessionProfile, _fileReader.GetCredentialProfile(_sampleSsoWithSsoSessionProfile.Name));
        }

        [Fact]
        public void DeleteProfileTest()
        {
            _sut.CreateOrUpdateProfile(_sampleStaticProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleStaticProfile.Name));

            _sut.DeleteProfile(_sampleStaticProfile.Name);
            Assert.Null(_fileReader.GetCredentialProfile(_sampleStaticProfile.Name));
        }

        [Fact]
        public void RenameProfileTest()
        {
            _sut.CreateOrUpdateProfile(_sampleStaticProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleStaticProfile.Name));

            _sut.RenameProfile(_sampleStaticProfile.Name, _sampleAlternateStaticProfile.Name);
            _fileReader.Load();

            Assert.Single(_fileReader.ProfileNames);
            Assert.Equal(_sampleAlternateStaticProfile.Name, _fileReader.ProfileNames.First());
            Assert.Null(_fileReader.GetCredentialProfile(_sampleStaticProfile.Name));
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleAlternateStaticProfile.Name));
        }

        [Fact]
        public void UpdateProfileTest()
        {
            _sut.CreateOrUpdateProfile(_sampleStaticProfile);
            Assert.Equal("access_key", _fileReader.GetCredentialProfile(_sampleStaticProfile.Name).Options.AccessKey);

            var profile = new CredentialProfile(_sampleStaticProfile.Name,
            new CredentialProfileOptions { AccessKey = "access_key_changed", SecretKey = "secret_key" });
            _sut.CreateOrUpdateProfile(profile);

            Assert.Equal("access_key_changed", _fileReader.GetCredentialProfile(_sampleStaticProfile.Name).Options.AccessKey);
            _fileReader.Load();
            Assert.Single(_fileReader.ProfileNames);
        }
    }
}
