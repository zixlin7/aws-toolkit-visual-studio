using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Amazon.AWSToolkit.Credentials.IO;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class SharedCredentialFileReaderTests : IDisposable
    {
        private const string InvalidProfileName = "invalid_profile";

        private static readonly string InvalidProfileText = new StringBuilder()
            .AppendLine($"[{InvalidProfileName}]")
            .AppendLine("aws_access_key_id=session_aws_access_key_id")
            .AppendLine("aws_session_token=session_aws_session_token")
            .ToString();

        private readonly SharedCredentialFileReader _fileReader;
        private readonly SharedCredentialFileTestFixture _fixture = new SharedCredentialFileTestFixture();

        public SharedCredentialFileReaderTests()
        {
            _fileReader = new SharedCredentialFileReader(_fixture.CredentialsFile);
        }

        [Fact]
        public void EmptyCredentials()
        {
            Assert.Null(_fileReader.ProfileNames);
            _fileReader.Load();
            Assert.Empty(_fileReader.ProfileNames);
            Assert.Null(_fileReader.GetCredentialProfileOptions(CredentialProfileTestHelper.BasicProfileName));
            Assert.Null(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));
        }


        [Fact]
        public void ValidCredentials()
        {
            Assert.Null(_fileReader.ProfileNames);
            _fixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.BasicProfile);
            _fixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.SessionProfile);

            _fileReader.Load();
            var credentialProfileOptions =
                _fileReader.GetCredentialProfileOptions(CredentialProfileTestHelper.BasicProfileName);

            Assert.Equal(2, _fileReader.ProfileNames.Count);
            Assert.NotNull(credentialProfileOptions);
            Assert.NotNull(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.SessionProfileName));
            Assert.Equal(CredentialProfileTestHelper.BasicProfile.Options.AccessKey,
                credentialProfileOptions.AccessKey);
        }

        [Fact]
        public void InvalidCredentials()
        {
            _fixture.SetupFileContents(InvalidProfileText);
            _fixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.BasicProfile);

            _fileReader.Load();
            var invalidProfileOptions = _fileReader.GetCredentialProfileOptions(InvalidProfileName);
            var allProfileNames = _fileReader.ProfileNames;
            var expectedProfiles = new List<string> {InvalidProfileName, CredentialProfileTestHelper.BasicProfileName};

            Assert.Equal(2, allProfileNames.Count);
            Assert.Equal(expectedProfiles, allProfileNames);

            Assert.Null(_fileReader.GetCredentialProfile(InvalidProfileName));
            Assert.NotNull(_fileReader.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName));
            Assert.NotNull(invalidProfileOptions);
            Assert.Equal("session_aws_access_key_id", invalidProfileOptions.AccessKey);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
