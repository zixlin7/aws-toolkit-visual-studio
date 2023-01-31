using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;
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

        public static TheoryData<string> GetBasicValidProfileNames() =>
            new TheoryData<string>()
            {
                CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name,
                CredentialProfileTestHelper.Basic.Valid.Token.Name,
            };

        [Theory]
        [MemberData(nameof(GetBasicValidProfileNames))]
        public void CreateToolkitCredentials_BasicProfiles(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.NotNull(GetFactory().CreateToolkitCredentials(identifier, null));
        }

        [Fact]
        public void CreateToolkitCredentials_AssumeRole_SourceProfile()
        {
            var profile = CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile;
            var identifier = CreateCredentialIdentifier(profile.Name);
            var toolkitCredentials = GetFactory().CreateToolkitCredentials(identifier, SampleRegion);

            AssertSourceProfileAssumeRoleCredentials(toolkitCredentials.GetAwsCredentials(), profile, SampleRegion.Id);
        }

        private static void AssertSourceProfileAssumeRoleCredentials(AWSCredentials awsCredentials,
            CredentialProfile expectedProfile, string expectedRegionId)
        {
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(awsCredentials);
            Assert.Equal(expectedRegionId, assumeRoleAwsCredentials.Options.Region);
            Assert.Equal(expectedProfile.Options.ExternalID, assumeRoleAwsCredentials.Options.ExternalId);
            Assert.Equal(expectedProfile.Options.RoleArn, assumeRoleAwsCredentials.RoleArn);
            Assert.Equal(expectedProfile.Options.RoleSessionName, assumeRoleAwsCredentials.RoleSessionName);
        }

        [Fact]
        public void CreateToolkitCredentials_AssumeRole_CredentialSource()
        {
            var profile = CredentialProfileTestHelper.AssumeRole.Valid.CredentialSource;
            var identifier = CreateCredentialIdentifier(profile.Name);
            var toolkitCredentials = GetFactory().CreateToolkitCredentials(identifier, SampleRegion);

            AssertEc2CredentialSourceAssumeRoleCredentials(toolkitCredentials.GetAwsCredentials(), profile, SampleRegion.Id);
        }

        private static void AssertEc2CredentialSourceAssumeRoleCredentials(AWSCredentials credentials,
            CredentialProfile expectedProfile, string expectedRegionId)
        {
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(expectedRegionId, assumeRoleAwsCredentials.Options.Region);
            Assert.Equal(expectedProfile.Options.RoleArn, assumeRoleAwsCredentials.RoleArn);
            Assert.IsType<ToolkitDefaultEc2InstanceCredentials>(assumeRoleAwsCredentials.SourceCredentials);
        }

        [Fact]
        public void CreateToolkitCredentials_AssumeMFARole()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Name);
            var toolkitCredentials = GetFactory().CreateToolkitCredentials(identifier, SampleRegion);

            AssertMfaCredentials(toolkitCredentials.GetAwsCredentials(), SampleRegion.Id);
        }

        private static void AssertMfaCredentials(AWSCredentials credentials, string expectedRegionId)
        {
            var assumeMfaRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Equal(expectedRegionId, assumeMfaRoleAwsCredentials.Options.Region);
            Assert.Equal(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Options.RoleArn,
                assumeMfaRoleAwsCredentials.RoleArn);
            Assert.Equal(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Options.MfaSerial,
                assumeMfaRoleAwsCredentials.Options.MfaSerialNumber);
        }

        [Fact]
        public void CreateToolkitCredentials_AssumeRole_NullRegion()
        {
            var identifier = CreateCredentialIdentifier(CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name);

            Assert.Throws<ArgumentNullException>(() => GetFactory().CreateToolkitCredentials(identifier, null));
        }

        [Fact]
        public void CreateToolkitCredentials_AssumeRole_DefaultSessionName()
        {
            var profileName = RegisterAssumeRoleProfileWithNoSessionName();

            var identifier = CreateCredentialIdentifier(profileName);
            var toolkitCredentials = GetFactory().CreateToolkitCredentials(identifier, SampleRegion);

            AssertAssumeRoleWithToolkitSessionName(toolkitCredentials.GetAwsCredentials());
        }

        private string RegisterAssumeRoleProfileWithNoSessionName()
        {
            string profileName = Guid.NewGuid().ToString();
            var sampleProfile = CredentialProfileTestHelper.AssumeRole.CreateSampleProfile(profileName);
            sampleProfile.Options.RoleSessionName = string.Empty;
            SampleProfiles.Add(sampleProfile);
            return profileName;
        }

        private static void AssertAssumeRoleWithToolkitSessionName(AWSCredentials credentials)
        {
            var assumeRoleAwsCredentials = Assert.IsType<ToolkitAssumeRoleAwsCredentials>(credentials);
            Assert.Contains("aws-toolkit-visualstudio-", assumeRoleAwsCredentials.RoleSessionName);
        }

        public static TheoryData<string> GetUnsupportedIdentifierProfileNames() =>
            new TheoryData<string>()
            {
                CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name,
                CredentialProfileTestHelper.Basic.Valid.Token.Name,
                CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name,
                CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name,
            };

        [Theory]
        [MemberData(nameof(GetUnsupportedIdentifierProfileNames))]
        public void CreateToolkitCredentials_UnsupportedIdentifier(string profileName)
        {
            var identifier = FakeCredentialIdentifier.Create(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateToolkitCredentials(identifier, null));
        }

        public static TheoryData<string> GetMissingProfileNames() =>
            new TheoryData<string>()
            {
                CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.Name,
                CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey.Name,
                CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name,
                CredentialProfileTestHelper.CredentialProcess.ValidProfile.Name,
                CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name,
            };

        [Theory]
        [MemberData(nameof(GetMissingProfileNames))]
        public void CreateToolkitCredentials_MissingProfile(string profileName)
        {
            ProfileHolder.Setup(x => x.GetProfile(profileName)).Returns((CredentialProfile) null);

            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<InvalidOperationException>(() => GetFactory().CreateToolkitCredentials(identifier, null));
        }

        public static TheoryData<string> GetInvalidProfileNames() =>
            new TheoryData<string>()
            {
                CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.Name,
                CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey.Name,
                CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name,
                CredentialProfileTestHelper.CredentialProcess.InvalidProfile.Name,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceDoesNotExist.Name,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoRegion.Name,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoStartUrl.Name,
            };

        [Theory]
        [MemberData(nameof(GetInvalidProfileNames))]
        public void CreateToolkitCredentials_InvalidProfile(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateToolkitCredentials(identifier, null));
        }

        public static TheoryData<string> GetInvalidAssumeRoleProfileNames() =>
            new TheoryData<string>()
            {
                CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.BadReference.Name,
                CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.Missing.Name,
                CredentialProfileTestHelper.AssumeRole.Invalid.CredentialSource.InvalidValue.Name,
                CredentialProfileTestHelper.AssumeRole.Invalid.CredentialSource.Unsupported.Name,
            };

        [Theory]
        [MemberData(nameof(GetInvalidAssumeRoleProfileNames))]
        public void CreateToolkitCredentials_InvalidSourceProfileReferences(string profileName)
        {
            var identifier = CreateCredentialIdentifier(profileName);
            Assert.Throws<ArgumentException>(() => GetFactory().CreateToolkitCredentials(identifier, SampleRegion));
        }

        [Fact]
        public void CreateToolkitCredentials_SsoSession_Valid()
        {
            var credentialId =
                CreateCredentialIdentifier(CredentialProfileTestHelper.SsoSession.Valid.SdkHydratedProfileReferencesTokenBasedSsoSession
                    .Name);

            var toolkitCredentials = GetFactory().CreateToolkitCredentials(credentialId, SampleRegion);

            Assert.True(toolkitCredentials.Supports(AwsConnectionType.AwsToken));
            Assert.False(toolkitCredentials.Supports(AwsConnectionType.AwsCredentials));
            Assert.NotNull(toolkitCredentials.GetTokenProvider());
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

            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Valid.SsoSessionProfile);
            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Valid.SdkHydratedProfileReferencesTokenBasedSsoSession);
            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceDoesNotExist);
            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoRegion);
            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoRegion);
            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoStartUrl);
            SampleProfiles.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoStartUrl);

            // By default, ProfileHolder returns profiles defined in SampleProfiles
            ProfileHolder.Setup(mock => mock.GetProfile(It.IsAny<string>()))
                .Returns<string>(profileName => SampleProfiles.FirstOrDefault(profile => profile.Name == profileName));
        }
    }
}
