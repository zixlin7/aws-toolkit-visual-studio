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

        public static IEnumerable<object[]> GetBasicValidProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.Token.Name };
        }

        [Theory]
        [MemberData(nameof(GetBasicValidProfileNames))]
        public void CreateAwsCredentials(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.NotNull(GetFactory().CreateAwsCredential(identifier, null));
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole_SourceProfile()
        {
            var profile = CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile;
            var identifier = CreateCredentialIdentifier(profile.Name);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(SampleRegion.Id, assumeRoleAwsCredentials.Options.Region);
            Assert.Equal(profile.Options.ExternalID, assumeRoleAwsCredentials.Options.ExternalId);
            Assert.Equal(profile.Options.RoleArn, assumeRoleAwsCredentials.RoleArn);
            Assert.Equal(profile.Options.RoleSessionName, assumeRoleAwsCredentials.RoleSessionName);
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole_CredentialSource()
        {
            var profile = CredentialProfileTestHelper.AssumeRole.Valid.CredentialSource;
            var identifier = CreateCredentialIdentifier(profile.Name);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(SampleRegion.Id, assumeRoleAwsCredentials.Options.Region);
            Assert.Equal(profile.Options.RoleArn, assumeRoleAwsCredentials.RoleArn);
            Assert.IsType<ToolkitDefaultEc2InstanceCredentials>(assumeRoleAwsCredentials.SourceCredentials);
        }

        [Fact]
        public void CreateAwsCredentials_AssumeMFARole()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Name);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeMfaRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(SampleRegion.Id, assumeMfaRoleAwsCredentials.Options.Region);
            Assert.Equal(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Options.RoleArn, assumeMfaRoleAwsCredentials.RoleArn);
            Assert.Equal(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Options.MfaSerial, assumeMfaRoleAwsCredentials.Options.MfaSerialNumber);
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole_NullRegion()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name);

            Assert.Throws<ArgumentNullException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        [Fact]
        public void CreateAwsCredentials_AssumeRole_DefaultSessionName()
        {
            string profileName = Guid.NewGuid().ToString();
            var sampleProfile = CredentialProfileTestHelper.AssumeRole.CreateSampleProfile(profileName);
            sampleProfile.Options.RoleSessionName = string.Empty;
            SampleProfiles.Add(sampleProfile);

            var identifier = CreateCredentialIdentifier(profileName);
            var credentials = GetFactory().CreateAwsCredential(identifier, SampleRegion);

            Assert.NotNull(credentials);
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Contains("aws-toolkit-visualstudio-", assumeRoleAwsCredentials.RoleSessionName);
        }

        public static IEnumerable<object[]> GetUnsupportedIdentifierProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.Token.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name };
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedIdentifierProfileNames))]
        public void CreateAwsCredential_UnsupportedIdentifier(string profileName)
        {
            var identifier = FakeCredentialIdentifier.Create(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        public static IEnumerable<object[]> GetMissingProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.CredentialProcess.ValidProfile.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name };
        }

        [Theory]
        [MemberData(nameof(GetMissingProfileNames))]
        public void CreateAwsCredential_MissingProfile(string profileName)
        {
            ProfileHolder.Setup(x => x.GetProfile(profileName)).Returns((CredentialProfile) null);

            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<InvalidOperationException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        public static IEnumerable<object[]> GetInvalidProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.CredentialProcess.InvalidProfile.Name };
        }

        [Theory]
        [MemberData(nameof(GetInvalidProfileNames))]
        public void CreateAwsCredential_InvalidProfile(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateAwsCredential(identifier, null));
        }

        public static IEnumerable<object[]> GetInvalidAssumeRoleProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.BadReference.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.Missing.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Invalid.CredentialSource.InvalidValue.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Invalid.CredentialSource.Unsupported.Name };
        }

        [Theory]
        [MemberData(nameof(GetInvalidAssumeRoleProfileNames))]
        public void CreateAwsCredential_InvalidSourceProfileReferences(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateAwsCredential(identifier, SampleRegion));
        }

        private void SetupSampleProfiles()
        {
            SampleProfiles.Add(CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey);
            SampleProfiles.Add(CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey);
            SampleProfiles.Add(CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey);

            SampleProfiles.Add(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret);
            SampleProfiles.Add(CredentialProfileTestHelper.Basic.Valid.Token);

            SampleProfiles.Add(CredentialProfileTestHelper.CredentialProcess.InvalidProfile);

            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRole.Valid.CredentialSource);
            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile);
            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.CredentialSource.InvalidValue);
            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.CredentialSource.Unsupported);
            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.BadReference);
            SampleProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.Missing);

            SampleProfiles.Add(CredentialProfileTestHelper.Mfa.Valid.MfaReference);

            // By default, ProfileHolder returns profiles defined in SampleProfiles
            ProfileHolder.Setup(mock => mock.GetProfile(It.IsAny<string>()))
                .Returns<string>(profileName => SampleProfiles.FirstOrDefault(profile => profile.Name == profileName));
        }
    }
}
