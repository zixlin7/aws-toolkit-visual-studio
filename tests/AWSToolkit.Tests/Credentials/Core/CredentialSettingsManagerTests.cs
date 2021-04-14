using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class CredentialSettingsManagerTests
    {

        private readonly Dictionary<string, ICredentialProviderFactory> _processorFactoryMapping = new Dictionary<string, ICredentialProviderFactory>();
        private readonly Mock<ICredentialProfileProcessor> _sdkProcessor = new Mock<ICredentialProfileProcessor>();
        private readonly Mock<ICredentialProfileProcessor> _sharedProcessor = new Mock<ICredentialProfileProcessor>();
        private readonly Mock<ICredentialProviderFactory> _sharedFactory = new Mock<ICredentialProviderFactory>();
        private readonly Mock<ICredentialProviderFactory> _sdkFactory = new Mock<ICredentialProviderFactory>();
        private readonly CredentialSettingsManager _credentialSettingsManager;

        public CredentialSettingsManagerTests()
        {
            _sharedFactory.Setup(x => x.GetCredentialProfileProcessor()).Returns(_sharedProcessor.Object);
            _sdkFactory.Setup(x => x.GetCredentialProfileProcessor()).Returns(_sdkProcessor.Object);
            _processorFactoryMapping.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, _sharedFactory.Object);
            _processorFactoryMapping.Add(SDKCredentialProviderFactory.SdkProfileFactoryId, _sdkFactory.Object);
            _credentialSettingsManager = new CredentialSettingsManager(_processorFactoryMapping);
        }

        [Fact]
        public void EmptyFactories_ThrowsError()
        {
            var settingsManager = new CredentialSettingsManager();
            var identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            Assert.Throws<ArgumentException>(() => settingsManager.CreateProfile(identifier, new ProfileProperties()));
        }

        [Fact]
        public void DeleteProfileTest()
        {
            var identifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            _credentialSettingsManager.DeleteProfile(identifier);
            _sharedProcessor.Verify(x => x.DeleteProfile(identifier), Times.Once);
        }

        [Fact]
        public void CreateProfileTest()
        {
            var identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            var properties = new ProfileProperties();
            _credentialSettingsManager.CreateProfile(identifier, properties);
            _sdkProcessor.Verify(x => x.CreateProfile(identifier, properties), Times.Once);
        }

        [Fact]
        public void RenameProfileTest()
        {
            var oldIdentifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            var newIdentifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.SessionProfileName);
            _credentialSettingsManager.RenameProfile(oldIdentifier, newIdentifier);
            _sharedProcessor.Verify(x => x.RenameProfile(oldIdentifier, newIdentifier), Times.Once);
        }

        [Fact]
        public void RenameProfile_ThrowsError()
        {
            var oldIdentifier = new SharedCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            var newIdentifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.SessionProfileName);
            Assert.Throws<NotSupportedException>(() => _credentialSettingsManager.RenameProfile(oldIdentifier, newIdentifier));
        }

        [Fact]
        public void UpdateProfileTest()
        {
            var identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            var properties = new ProfileProperties();
            _credentialSettingsManager.UpdateProfile(identifier, properties);
            _sdkProcessor.Verify(x => x.UpdateProfile(identifier, properties), Times.Once);
        }

        [Fact]
        public void GetProfilePropertiesTest()
        {
            var identifier = new SDKCredentialIdentifier(CredentialProfileTestHelper.BasicProfileName);
            _credentialSettingsManager.GetProfileProperties(identifier);
            _sdkProcessor.Verify(x => x.GetProfileProperties(identifier), Times.Once);
        }
    }
}
