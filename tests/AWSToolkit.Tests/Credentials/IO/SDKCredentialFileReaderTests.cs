using System;
using System.Collections.Generic;
using System.Text;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;
using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class SDKCredentialFileReaderTests : IDisposable
    {
        private const string SomeOtherKey = "SomeOtherKey";
        private const string SomeOtherValue = "some_other_value";
        private static readonly Guid UniqueKey = Guid.NewGuid();

        private static readonly string BasicProfileText = new StringBuilder()
            .AppendLine("{")
            .AppendLine("    \"" + UniqueKey + "\" : {")
            .AppendLine("        \"DisplayName\" : \"BasicProfile\",")
            .AppendLine("        \"SessionType\" : \"AWS\",")
            .AppendLine("        \"AWSAccessKey\" : \"access_key_id\",")
            .AppendLine("        \"AWSSecretKey\" : \"secret_key_id\",")
            .AppendLine("        \"" + SomeOtherKey + "\" : \"" + SomeOtherValue + "\",")
            .AppendLine("    }")
            .AppendLine("}").ToString();

        private static readonly string InvalidProfileText = new StringBuilder()
            .AppendLine("{")
            .AppendLine("    \"" + UniqueKey + "\" : {")
            .AppendLine("        \"DisplayName\" : \"InvalidProfile\",")
            .AppendLine("        \"SessionType\" : \"AWS\",")
            .AppendLine("        \"AWSAccessKey\" : \"access_key_id\",")
            .AppendLine("    }")
            .AppendLine("}").ToString();

        private static readonly CredentialProfile TestProfile = new CredentialProfile("TestProfile",
            new CredentialProfileOptions {AccessKey = "access_key", SecretKey = "secret_key"});

        private readonly SDKCredentialFileReader _fileReader;
        private readonly SDKCredentialFileTestFixture _fixture = new SDKCredentialFileTestFixture();

        public SDKCredentialFileReaderTests()
        {
            _fileReader = new SDKCredentialFileReader(_fixture.ProfileStore, _fixture.Manager);
        }

        [Fact]
        public void EmptyCredentials()
        {
            Assert.Null(_fileReader.ProfileNames);
            _fileReader.Load();
            Assert.Empty(_fileReader.ProfileNames);
            Assert.Null(_fileReader.GetCredentialProfileOptions("BasicProfile"));
            Assert.Null(_fileReader.GetCredentialProfile("BasicProfile"));
        }

        [Fact]
        public void ValidCredentials()
        {
            Assert.Null(_fileReader.ProfileNames);
            _fixture.SetFileContents(BasicProfileText);
            _fixture.ProfileStore.RegisterProfile(TestProfile);

            _fileReader.Load();
            var credentialProfileOptions = _fileReader.GetCredentialProfileOptions(TestProfile.Name);

            Assert.Equal(2, _fileReader.ProfileNames.Count);
            Assert.NotNull(credentialProfileOptions);
            Assert.NotNull(_fileReader.GetCredentialProfile(TestProfile.Name));
            Assert.Equal(TestProfile.Options.AccessKey, credentialProfileOptions.AccessKey);
        }

        [Fact]
        public void InvalidCredentials()
        {
            _fixture.SetFileContents(InvalidProfileText);
            _fixture.ProfileStore.RegisterProfile(TestProfile);

            _fileReader.Load();
            var invalidProfileOptions = _fileReader.GetCredentialProfileOptions("InvalidProfile");
            var allProfileNames = _fileReader.ProfileNames;
            var expectedProfiles = new List<string> {"InvalidProfile", "TestProfile"};

            Assert.Equal(2, allProfileNames.Count);
            Assert.Equal(expectedProfiles, allProfileNames);

            Assert.Null(_fileReader.GetCredentialProfile("InvalidProfile"));
            Assert.NotNull(_fileReader.GetCredentialProfile(TestProfile.Name));
            Assert.NotNull(invalidProfileOptions);
            Assert.Equal("access_key_id", invalidProfileOptions.AccessKey);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
