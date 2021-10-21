using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime.CredentialManagement;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    /// <summary>
    /// Tests that are common to <see cref="SDKCredentialProviderFactoryTests"/>
    /// and <see cref="SharedCredentialProviderFactoryTests"/>.
    /// </summary>
    public abstract class BaseFileCredentialProviderFactoryTests
    {
        private static readonly ToolkitRegion SampleRegion = new ToolkitRegion{Id= "us-west-2", PartitionId = "partition", DisplayName = "region-name"};
        protected readonly Mock<IProfileHolder> ProfileHolder = new Mock<IProfileHolder>();
        protected readonly List<CredentialProfile> SampleProfiles = new List<CredentialProfile>();
        protected readonly Mock<IAWSToolkitShellProvider> ToolkitShell = new Mock<IAWSToolkitShellProvider>();

        protected BaseFileCredentialProviderFactoryTests()
        {
            SetupSampleProfiles();
        }

        protected abstract ProfileCredentialProviderFactory GetFactory();
        protected abstract ICredentialIdentifier CreateCredentialIdentifier(string profileName);

        [Theory]
        [InlineData(CredentialProfileTestHelper.BasicProfileName)]
        [InlineData(CredentialProfileTestHelper.SessionProfileName)]
        public void CreateAwsCredentials(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.NotNull(GetFactory().CreateAwsCredential(identifier, null));
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.AssumeRoleProfileName);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(SampleRegion.Id, assumeRoleAwsCredentials.Options.Region);
            Assert.Equal(CredentialProfileTestHelper.AssumeRoleProfile.Options.ExternalID, assumeRoleAwsCredentials.Options.ExternalId);
            Assert.Equal(CredentialProfileTestHelper.AssumeRoleProfile.Options.RoleArn, assumeRoleAwsCredentials.RoleArn);
            Assert.Equal(CredentialProfileTestHelper.AssumeRoleProfile.Options.RoleSessionName, assumeRoleAwsCredentials.RoleSessionName);
        }

        [Fact]
        public void CreateAwsCredentials_AssumeMFARole()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.MFAProfileName);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeMfaRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(SampleRegion.Id, assumeMfaRoleAwsCredentials.Options.Region);
            Assert.Equal(CredentialProfileTestHelper.MFAProfile.Options.RoleArn, assumeMfaRoleAwsCredentials.RoleArn);
            Assert.Equal(CredentialProfileTestHelper.MFAProfile.Options.MfaSerial, assumeMfaRoleAwsCredentials.Options.MfaSerialNumber);
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole_NullRegion()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.AssumeRoleProfileName);

            Assert.Throws<ArgumentNullException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole_DefaultSessionName()
        {
            string profileName = Guid.NewGuid().ToString();
            var sampleProfile = CredentialProfileTestHelper.CreateSampleAssumeRoleProfile(profileName);
            sampleProfile.Options.RoleSessionName = string.Empty;
            SampleProfiles.Add(sampleProfile);

            var identifier = CreateCredentialIdentifier(profileName);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Contains("aws-toolkit-visualstudio-", assumeRoleAwsCredentials.RoleSessionName);
        }

        [Theory]
        [InlineData(CredentialProfileTestHelper.BasicProfileName)]
        [InlineData(CredentialProfileTestHelper.SessionProfileName)]
        [InlineData(CredentialProfileTestHelper.AssumeRoleProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidSessionProfileName)]
        public void CreateAwsCredential_UnsupportedIdentifier(string profileName)
        {
            var identifier = FakeCredentialIdentifier.Create(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        [Theory]
        [InlineData(CredentialProfileTestHelper.InvalidSessionProfileName)]
        [InlineData(CredentialProfileTestHelper.CredentialProcessProfileName)]
        [InlineData(CredentialProfileTestHelper.AssumeRoleProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidBasicProfileName)]
        public void CreateAwsCredential_MissingProfile(string profileName)
        {
            ProfileHolder.Setup(x => x.GetProfile(profileName)).Returns((CredentialProfile) null);

            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<InvalidOperationException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        [Theory]
        [InlineData(CredentialProfileTestHelper.InvalidSessionProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidProcessProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidBasicProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidProfileName)]
        public void CreateAwsCredential_InvalidProfile(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        [Theory]
        [InlineData(CredentialProfileTestHelper.InvalidAssumeRoleProfileBadSourceProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidAssumeRoleProfileNoSourceProfileName)]
        public void CreateAwsCredential_InvalidSourceProfileReferences(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<InvalidOperationException>(() => GetFactory().CreateAwsCredential(identifier, SampleRegion));
        }

        private void SetupSampleProfiles()
        {
            SampleProfiles.Add(CredentialProfileTestHelper.InvalidProfile);

            SampleProfiles.Add(CredentialProfileTestHelper.BasicProfile);
            SampleProfiles.Add(CredentialProfileTestHelper.InvalidBasicProfile);

            SampleProfiles.Add(CredentialProfileTestHelper.SessionProfile);
            SampleProfiles.Add(CredentialProfileTestHelper.InvalidSessionProfile);

            SampleProfiles.Add(CredentialProfileTestHelper.InvalidCredentialProcess);

            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRoleProfile);
            SampleProfiles.Add(CredentialProfileTestHelper.InvalidAssumeRoleProfileBadSourceProfile);
            SampleProfiles.Add(CredentialProfileTestHelper.InvalidAssumeRoleProfileNoSourceProfile);

            SampleProfiles.Add(CredentialProfileTestHelper.MFAProfile);

            // By default, ProfileHolder returns profiles defined in SampleProfiles
            ProfileHolder.Setup(mock => mock.GetProfile(It.IsAny<string>()))
                .Returns<string>(profileName => SampleProfiles.FirstOrDefault(profile => profile.Name == profileName));
        }
    }
}
