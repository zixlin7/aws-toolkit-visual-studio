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

        private readonly CredentialProfile _sampleProfile = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret;

        private readonly CredentialProfile _sampleAlternateProfile = CredentialProfileTestHelper.Basic.Valid.Token;

        public MemoryCredentialFileWriterTests()
        {
            _file = new MemoryCredentialsFile();
            _sut = new MemoryCredentialFileWriter(_file);
            _fileReader = new MemoryCredentialFileReader(_file);
            _fileReader.Load();
        }

        [Fact]
        public void CreateProfileTest()
        {
            Assert.Empty(_fileReader.ProfileNames);
            _sut.CreateOrUpdateProfile(_sampleProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleProfile.Name));
        }

        [Fact]
        public void DeleteProfileTest()
        {
            _sut.CreateOrUpdateProfile(_sampleProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleProfile.Name));

            _sut.DeleteProfile(_sampleProfile.Name);
            Assert.Null(_fileReader.GetCredentialProfile(_sampleProfile.Name));
        }

        [Fact]
        public void RenameProfileTest()
        {
            _sut.CreateOrUpdateProfile(_sampleProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleProfile.Name));

            _sut.RenameProfile(_sampleProfile.Name, _sampleAlternateProfile.Name);
            _fileReader.Load();

            Assert.Single(_fileReader.ProfileNames);
            Assert.Equal(_sampleAlternateProfile.Name, _fileReader.ProfileNames.First());
            Assert.Null(_fileReader.GetCredentialProfile(_sampleProfile.Name));
            Assert.NotNull(_fileReader.GetCredentialProfile(_sampleAlternateProfile.Name));
        }

        [Fact]
        public void UpdateProfileTest()
        {
            _sut.CreateOrUpdateProfile(_sampleProfile);
            Assert.Equal("access_key", _fileReader.GetCredentialProfile(_sampleProfile.Name).Options.AccessKey);

            var profile = new CredentialProfile(_sampleProfile.Name,
            new CredentialProfileOptions { AccessKey = "access_key_changed", SecretKey = "secret_key" });
            _sut.CreateOrUpdateProfile(profile);

            Assert.Equal("access_key_changed", _fileReader.GetCredentialProfile(_sampleProfile.Name).Options.AccessKey);
            _fileReader.Load();
            Assert.Single(_fileReader.ProfileNames);
        }
    }
}
