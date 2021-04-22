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
            newProfiles.ValidProfiles.Add(CredentialProfileTestHelper.CredentialProcessProfileName,
                CredentialProfileTestHelper.CredentialProcessProfile);
            newProfiles.ValidProfiles.Add(CredentialProfileTestHelper.BasicProfileName,
                CredentialProfileTestHelper.BasicProfile);
            var oldProfiles = new Dictionary<string, CredentialProfile>
            {
                [CredentialProfileTestHelper.SessionProfileName] = CredentialProfileTestHelper.SessionProfile,
                [CredentialProfileTestHelper.BasicProfileName] = new CredentialProfile(
                    CredentialProfileTestHelper.BasicProfileName,
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
            profiles.ValidProfiles.Add(CredentialProfileTestHelper.BasicProfileName,
                CredentialProfileTestHelper.BasicProfile);
            profiles.ValidProfiles.Add(CredentialProfileTestHelper.CredentialProcessProfileName,
                CredentialProfileTestHelper.CredentialProcessProfile);
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

        [Theory]
        [InlineData(CredentialProfileTestHelper.BasicProfileName)]
        [InlineData(CredentialProfileTestHelper.SessionProfileName)]
        [InlineData(CredentialProfileTestHelper.AssumeRoleProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidBasicProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidSessionProfileName)]
        public void LoginIsNotRequired(string identifierName)
        {
            var identifier = new SharedCredentialIdentifier(identifierName);
            Assert.False(_factory.IsLoginRequired(identifier));
        }

        [Fact]
        public void LoginIsRequired()
        {
            var identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.MFAProfileName);
            Assert.True(_factory.IsLoginRequired(identifier));

            identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.MFAExternalSessionProfileName);
            Assert.True(_factory.IsLoginRequired(identifier));

            identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.SSOProfileName);
            Assert.True(_factory.IsLoginRequired(identifier));

            identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.CredentialProcessProfileName);
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
            _reader.Setup(x => x.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName)).Returns((CredentialProfile)null);
            var identifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            Assert.Throws<ArgumentException>(() => _factory.GetProfileProperties(identifier));
            _reader.Verify(x => x.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName), Times.Once);
        }

        [Fact]
        public void VerifyGetProfileCall_ReturnsResult()
        {
            _reader.Setup(x => x.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName)).Returns(CredentialProfileTestHelper.BasicProfile);
            var identifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            Assert.NotNull(_factory.GetProfileProperties(identifier));
            _reader.Verify(x => x.GetCredentialProfile(CredentialProfileTestHelper.BasicProfileName), Times.Once);
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
            _profiles[CredentialProfileTestHelper.BasicProfileName] = CredentialProfileTestHelper.BasicProfile;
            _profiles[CredentialProfileTestHelper.SessionProfileName] = CredentialProfileTestHelper.SessionProfile;
            _profiles[CredentialProfileTestHelper.MFAProfileName] = CredentialProfileTestHelper.MFAProfile;
            _profiles[CredentialProfileTestHelper.MFAExternalSessionProfileName] = CredentialProfileTestHelper.MFAExternalSessionProfile;
            _profiles[CredentialProfileTestHelper.SSOProfileName] = CredentialProfileTestHelper.SSOProfile;
            _profiles[CredentialProfileTestHelper.AssumeRoleProfileName] = CredentialProfileTestHelper.AssumeRoleProfile;
            _profiles[CredentialProfileTestHelper.CredentialProcessProfileName] = CredentialProfileTestHelper.CredentialProcessProfile;
        }

        public void Dispose()
        {
            _factory?.Dispose();
            _fixture?.Dispose();
            _exposedTestFactory?.Dispose();
        }
    }
}
