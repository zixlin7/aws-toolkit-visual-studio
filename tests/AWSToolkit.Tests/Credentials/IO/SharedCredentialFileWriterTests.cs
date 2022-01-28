using System;
using System.Linq;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class SharedCredentialFileWriterTests : IDisposable
    {
        private static readonly CredentialProfile SampleProfile = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret;
        private static readonly CredentialProfile SampleAlternateProfile = CredentialProfileTestHelper.Basic.Valid.Token;

        private readonly SharedCredentialFileReader _fileReader;
        private readonly SharedCredentialFileWriter _fileWriter;
        private readonly SharedCredentialFileTestFixture _fixture = new SharedCredentialFileTestFixture();

        public SharedCredentialFileWriterTests()
        {
            _fileReader = new SharedCredentialFileReader(_fixture.CredentialsFile);
            _fileWriter = new SharedCredentialFileWriter(_fixture.CredentialsFile);
            _fileReader.Load();
        }

        [Fact]
        public void CreateProfileTest()
        {
            Assert.Empty(_fileReader.ProfileNames);
            _fileWriter.CreateOrUpdateProfile(SampleProfile);
            Assert.NotNull( _fileReader.GetCredentialProfile(SampleProfile.Name));
        }

        [Fact]
        public void DeleteProfileTest()
        {
            _fileWriter.CreateOrUpdateProfile(SampleProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(SampleProfile.Name));

            _fileWriter.DeleteProfile(SampleProfile.Name);
            Assert.Null(_fileReader.GetCredentialProfile(SampleProfile.Name));
        }

        [Fact]
        public void RenameProfileTest()
        {
            _fileWriter.CreateOrUpdateProfile(SampleProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(SampleProfile.Name));

            _fileWriter.RenameProfile(SampleProfile.Name, SampleAlternateProfile.Name);
            _fileReader.Load();

            Assert.Single(_fileReader.ProfileNames);
            Assert.Equal(SampleAlternateProfile.Name,_fileReader.ProfileNames.First());
            Assert.Null(_fileReader.GetCredentialProfile(SampleProfile.Name));
            Assert.NotNull(_fileReader.GetCredentialProfile(SampleAlternateProfile.Name));
        }


        [Fact]
        public void UpdateProfileTest()
        {
            _fileWriter.CreateOrUpdateProfile(SampleProfile);
            Assert.Equal("access_key", _fileReader.GetCredentialProfile(SampleProfile.Name).Options.AccessKey);

            var profile = new CredentialProfile(SampleProfile.Name,
            new CredentialProfileOptions { AccessKey = "access_key_changed", SecretKey = "secret_key" });
            _fileWriter.CreateOrUpdateProfile(profile);

            Assert.Equal("access_key_changed",_fileReader.GetCredentialProfile(SampleProfile.Name).Options.AccessKey);
            _fileReader.Load();
            Assert.Single(_fileReader.ProfileNames);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
