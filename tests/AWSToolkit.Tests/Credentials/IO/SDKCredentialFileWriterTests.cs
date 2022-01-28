using System;
using System.Linq;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    [Collection(SdkCredentialCollectionDefinition.NonParallelTests)]
    public class SDKCredentialFileWriterTests : IDisposable
    {
        private static readonly CredentialProfile SampleProfile = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret;
        private static readonly CredentialProfile SampleAlternateProfile = CredentialProfileTestHelper.Basic.Valid.Token;

        private readonly SDKCredentialFileReader _fileReader;
        private readonly SDKCredentialFileTestFixture _fixture = new SDKCredentialFileTestFixture();
        private readonly SDKCredentialFileWriter _fileWriter;
       
        public SDKCredentialFileWriterTests()
        {
            _fileReader = new SDKCredentialFileReader(_fixture.ProfileStore, _fixture.Manager);
            _fileWriter = new SDKCredentialFileWriter(_fixture.ProfileStore);
            _fileReader.Load();
        }

        [Fact]
        public void CreateProfileTest()
        {
            Assert.Empty(_fileReader.ProfileNames);
            _fileWriter.CreateOrUpdateProfile(SampleProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(SampleProfile.Name));
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
            Assert.Equal(SampleAlternateProfile.Name, _fileReader.ProfileNames.First());
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

            Assert.Equal("access_key_changed", _fileReader.GetCredentialProfile(SampleProfile.Name).Options.AccessKey);
            _fileReader.Load();
            Assert.Single(_fileReader.ProfileNames);
        }


        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
