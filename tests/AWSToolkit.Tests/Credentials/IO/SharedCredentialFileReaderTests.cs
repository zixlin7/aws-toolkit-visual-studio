using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class SharedCredentialFileReaderTests : IDisposable
    {
        private static readonly CredentialProfile SampleProfile = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret;
        private static readonly CredentialProfile SampleAlternateProfile = CredentialProfileTestHelper.Basic.Valid.Token;

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
            Assert.Null(_fileReader.GetCredentialProfileOptions(SampleProfile.Name));
            Assert.Null(_fileReader.GetCredentialProfile(SampleProfile.Name));
        }


        [Fact]
        public void ValidCredentials()
        {
            Assert.Null(_fileReader.ProfileNames);
            _fixture.CredentialsFile.RegisterProfile(SampleProfile);
            _fixture.CredentialsFile.RegisterProfile(SampleAlternateProfile);

            _fileReader.Load();
            var credentialProfileOptions =
                _fileReader.GetCredentialProfileOptions(SampleProfile.Name);

            Assert.Equal(2, _fileReader.ProfileNames.Count());
            Assert.NotNull(credentialProfileOptions);
            Assert.NotNull(_fileReader.GetCredentialProfile(SampleAlternateProfile.Name));
            Assert.Equal(SampleProfile.Options.AccessKey,
                credentialProfileOptions.AccessKey);
        }

        [Fact]
        public void InvalidCredentials()
        {
            _fixture.SetupFileContents(InvalidProfileText);
            _fixture.CredentialsFile.RegisterProfile(SampleProfile);

            _fileReader.Load();
            var invalidProfileOptions = _fileReader.GetCredentialProfileOptions(InvalidProfileName);
            var allProfileNames = _fileReader.ProfileNames;
            var expectedProfiles = new List<string> {InvalidProfileName, SampleProfile.Name};

            Assert.Equal(2, allProfileNames.Count());
            Assert.Equal(expectedProfiles, allProfileNames);

            Assert.Null(_fileReader.GetCredentialProfile(InvalidProfileName));
            Assert.NotNull(_fileReader.GetCredentialProfile(SampleProfile.Name));
            Assert.NotNull(invalidProfileOptions);
            Assert.Equal("session_aws_access_key_id", invalidProfileOptions.AccessKey);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
