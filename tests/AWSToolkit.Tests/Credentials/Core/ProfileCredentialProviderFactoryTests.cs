using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;
using AWSToolkit.Tests.Credentials.IO;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class ProfileCredentialProviderFactoryTests :IDisposable
    {
        private readonly ProfileCredentialProviderFactory _factory;
        private readonly Dictionary<string, CredentialProfile> _profiles = new Dictionary<string, CredentialProfile>();
        private readonly Mock<ICredentialFileReader> _reader = new Mock<ICredentialFileReader>();
        private readonly Mock<ICredentialFileWriter> _writer = new Mock<ICredentialFileWriter>();
        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();
        private readonly SharedCredentialIdentifier _sampleIdentifier =  new SharedCredentialIdentifier("profile");
        private readonly SharedCredentialFileTestFixture _fixture = new SharedCredentialFileTestFixture();
        private readonly TestProfileCredentialProviderFactory _exposedTestFactory;

        public ProfileCredentialProviderFactoryTests()
        {
            SetupProfiles();
            var holder = new ProfileHolder(_profiles);
            _factory = new SharedCredentialProviderFactory(holder, _reader.Object, _writer.Object, _toolkitShell.Object);
            _exposedTestFactory = new TestProfileCredentialProviderFactory();
        }

        [Fact]
        public void RaiseCredentialEventWithNoChanges()
        {
            var newProfiles = new Profiles();
            foreach (var nameToProfile in _profiles)
            {
                newProfiles.ValidProfiles.Add(nameToProfile.Key, nameToProfile.Value);
            }

            var receivedEvent = Assert.Raises<CredentialChangeEventArgs>(
                a => _exposedTestFactory.CredentialsChanged += a,
                a => _exposedTestFactory.CredentialsChanged -= a,
                () => _exposedTestFactory.ExposedCreateCredentialChangeEvent(_profiles, newProfiles));
            Assert.NotNull(receivedEvent);
            Assert.Empty(receivedEvent.Arguments.Added);
            Assert.Empty(receivedEvent.Arguments.Removed);
            Assert.Empty(receivedEvent.Arguments.Modified);
        }

        [Fact]
        public void RaiseCredentialEventWithChanges()
        {
            var newProfiles = new Profiles();
            newProfiles.ValidProfiles.Add(CredentialProfileTestHelper.CredentialProcess.ValidProfile.Name,
                CredentialProfileTestHelper.CredentialProcess.ValidProfile);
            newProfiles.ValidProfiles.Add(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name,
                CredentialProfileTestHelper.Basic.Valid.AccessAndSecret);
            var oldProfiles = new Dictionary<string, CredentialProfile>
            {
                [CredentialProfileTestHelper.Basic.Valid.Token.Name] = CredentialProfileTestHelper.Basic.Valid.Token,
                [CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name] = new CredentialProfile(
                    CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name,
                    new CredentialProfileOptions {AccessKey = "sdk_access_key", SecretKey = "sdk_secret_key"})
            };

            var receivedEvent = Assert.Raises<CredentialChangeEventArgs>(
                a => _exposedTestFactory.CredentialsChanged += a,
                a => _exposedTestFactory.CredentialsChanged -= a,
                () => _exposedTestFactory.ExposedCreateCredentialChangeEvent(oldProfiles, newProfiles));
            Assert.NotNull(receivedEvent);
            Assert.Single(receivedEvent.Arguments.Added);
            Assert.Single(receivedEvent.Arguments.Removed);
            Assert.Single(receivedEvent.Arguments.Modified);
        }

        [Fact]
        public void EnsureUniqueKeyAssigned()
        {
            var profiles = new Profiles();
            profiles.ValidProfiles.Add(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name,
                CredentialProfileTestHelper.Basic.Valid.AccessAndSecret);
            profiles.ValidProfiles.Add(CredentialProfileTestHelper.CredentialProcess.ValidProfile.Name,
                CredentialProfileTestHelper.CredentialProcess.ValidProfile);
            var exposedFactory = new TestProfileCredentialProviderFactory(_writer.Object);
            exposedFactory.ExposedEnsureUniqueKeyAssigned(profiles);
           _writer.Verify(x => x.EnsureUniqueKeyAssigned(It.IsAny<CredentialProfile>()), Times.Exactly(2));
        }

        [Fact]
        public void EnsureUniqueKeyAssigned_WhenNoValidProfiles()
        {
            var profiles = new Profiles();
            var exposedFactory = new TestProfileCredentialProviderFactory(_writer.Object);
            exposedFactory.ExposedEnsureUniqueKeyAssigned(profiles);
            _writer.Verify(x => x.EnsureUniqueKeyAssigned(It.IsAny<CredentialProfile>()), Times.Never);
        }

        [Fact]
        public void CreateCredentialProfile()
        {
            var guid = Guid.NewGuid();
            var properties = new ProfileProperties
            {
                AccessKey ="access-key",
                UniqueKey = guid.ToString(),
                Region = RegionEndpoint.USEast1.SystemName
            };
            var profile = _exposedTestFactory.ExposeCreateCredentialProfile("sample", properties);
            Assert.NotNull(profile);
            Assert.Equal("access-key", profile.Options.AccessKey);
            Assert.Null(profile.Options.SecretKey);
            Assert.Equal(RegionEndpoint.USEast1, profile.Region);
            Assert.Equal(guid.ToString(), CredentialProfileUtils.GetUniqueKey(profile));
        }

        [Fact]
        public void CreateCredentialProfile_WhenProfilePropertiesNull()
        {
            Assert.Null(_exposedTestFactory.ExposeCreateCredentialProfile("name", null));
        }

        public static IEnumerable<object[]> GetNonLoginBasedProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.Token.Name };
            yield return new object[] { CredentialProfileTestHelper.Saml.ValidProfile.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Valid.CredentialSource.Name };
            yield return new object[] { CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name };
        }

        [Theory]
        [MemberData(nameof(GetNonLoginBasedProfileNames))]
        public void LoginIsNotRequired(string identifierName)
        {
            var identifier = new SharedCredentialIdentifier(identifierName);
            Assert.False(_factory.IsLoginRequired(identifier));
        }

        [Fact]
        public void LoginIsRequired()
        {
            var identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.Mfa.Valid.MfaReference.Name);
            Assert.True(_factory.IsLoginRequired(identifier));

            identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.Mfa.Valid.ExternalSession.Name);
            Assert.True(_factory.IsLoginRequired(identifier));

            identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.Sso.ValidProfile.Name);
            Assert.True(_factory.IsLoginRequired(identifier));

            identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.CredentialProcess.ValidProfile.Name);
            Assert.True(_factory.IsLoginRequired(identifier));
        }

        [Fact]
        public void VerifyCreateProfileCall()
        {
            var properties = new ProfileProperties();
            _factory.CreateProfile(_sampleIdentifier, properties);
            _writer.Verify(x=> x.CreateOrUpdateProfile(It.IsAny<CredentialProfile>()), Times.Once);
        }


        [Fact]
        public void VerifyDeleteProfileCall()
        {
            _factory.DeleteProfile(_sampleIdentifier);
            _writer.Verify(x => x.DeleteProfile("profile"), Times.Once);
        }


        [Fact]
        public void VerifyUpdateProfileCall()
        {
            var properties = new ProfileProperties();
            _factory.UpdateProfile(_sampleIdentifier, properties);
            _writer.Verify(x => x.CreateOrUpdateProfile(It.IsAny<CredentialProfile>()), Times.Once);
        }


        [Fact]
        public void VerifyGetProfileCall_ThrowsException()
        {
            _reader.Setup(x => x.GetCredentialProfile(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name)).Returns((CredentialProfile)null);
            var identifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name);
            Assert.Throws<ArgumentException>(() => _factory.GetProfileProperties(identifier));
            _reader.Verify(x => x.GetCredentialProfile(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name), Times.Once);
        }

        [Fact]
        public void VerifyGetProfileCall_ReturnsResult()
        {
            _reader.Setup(x => x.GetCredentialProfile(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name)).Returns(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret);
            var identifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name);
            Assert.NotNull(_factory.GetProfileProperties(identifier));
            _reader.Verify(x => x.GetCredentialProfile(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name), Times.Once);
        }

        [Fact]
        public void VerifyRenameProfileCall()
        {
            var identifierNew = new SharedCredentialIdentifier("newprofile");
            _factory.RenameProfile(_sampleIdentifier, identifierNew);
            _writer.Verify(x => x.RenameProfile("profile", "newprofile"), Times.Once);
        }

        [Fact]
        public void GetInitialCredentialIdentifiers()
        {
            var expectedIdentifiers = _profiles.Keys;
            Assert.Equal(expectedIdentifiers, _factory.GetCredentialIdentifiers().Select(x => x.ProfileName).ToList());
        }

        private void SetupProfiles()
        {
            foreach (var credentialProfile in new CredentialProfile[]
                     {
                         CredentialProfileTestHelper.Basic.Valid.AccessAndSecret,
                         CredentialProfileTestHelper.Basic.Valid.Token,
                         CredentialProfileTestHelper.Saml.ValidProfile,
                         CredentialProfileTestHelper.Mfa.Valid.MfaReference,
                         CredentialProfileTestHelper.Mfa.Valid.ExternalSession,
                         CredentialProfileTestHelper.Sso.ValidProfile,
                         CredentialProfileTestHelper.AssumeRole.Valid.CredentialSource,
                         CredentialProfileTestHelper.AssumeRole.Valid.SourceProfile,
                         CredentialProfileTestHelper.CredentialProcess.ValidProfile,
                     })
            {
                _profiles[credentialProfile.Name] = credentialProfile;
            }
        }

        public void Dispose()
        {
            _factory?.Dispose();
            _fixture?.Dispose();
            _exposedTestFactory?.Dispose();
        }
    }
}
