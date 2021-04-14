using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;
using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class SDKCredentialFileWriterTests : IDisposable
    {

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
            _fileWriter.CreateOrUpdateProfile(CredentialProfileTestHelper.BasicProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));
        }

        [Fact]
        public void DeleteProfileTest()
        {
            _fileWriter.CreateOrUpdateProfile(CredentialProfileTestHelper.BasicProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));

            _fileWriter.DeleteProfile(CredentialProfileTestHelper.BasicProfileName);
            Assert.Null(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));
        }

        [Fact]
        public void RenameProfileTest()
        {
            _fileWriter.CreateOrUpdateProfile(CredentialProfileTestHelper.BasicProfile);
            Assert.NotNull(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));

            _fileWriter.RenameProfile(CredentialProfileTestHelper.BasicProfileName, CredentialProfileTestHelper.SessionProfileName);
            _fileReader.Load();

            Assert.Single(_fileReader.ProfileNames);
            Assert.Equal(CredentialProfileTestHelper.SessionProfileName, _fileReader.ProfileNames.First());
            Assert.Null(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));
            Assert.NotNull(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.SessionProfileName));
        }


        [Fact]
        public void UpdateProfileTest()
        {
            _fileWriter.CreateOrUpdateProfile(CredentialProfileTestHelper.BasicProfile);
            Assert.Equal("access_key", _fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName).Options.AccessKey);

            var profile = new CredentialProfile(CredentialProfileTestHelper.BasicProfileName,
            new CredentialProfileOptions { AccessKey = "access_key_changed", SecretKey = "secret_key" });
            _fileWriter.CreateOrUpdateProfile(profile);

            Assert.Equal("access_key_changed", _fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName).Options.AccessKey);
            _fileReader.Load();
            Assert.Single(_fileReader.ProfileNames);
        }


        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
