using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class CredentialManagerTest
    {
        private readonly Dictionary<string, ICredentialProviderFactory> _providerFactoryMapping =
            new Dictionary<string, ICredentialProviderFactory>();
        private readonly ConcurrentDictionary<string, ICredentialIdentifier> _identifiers = new ConcurrentDictionary<string, ICredentialIdentifier>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AWSCredentials>> _awsCache =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, AWSCredentials>>();

        private readonly Mock<ICredentialProviderFactory> _sharedFactory = new Mock<ICredentialProviderFactory>();
        private readonly CredentialManager _credentialManager;
        private readonly SharedCredentialIdentifier _sharedIdentifier1 = new SharedCredentialIdentifier("testshared1");
        private readonly SharedCredentialIdentifier _sharedIdentifier2 = new SharedCredentialIdentifier("testshared2");
        private readonly SDKCredentialIdentifier _sdkIdentifier1 = new SDKCredentialIdentifier("testsdk1");
        private readonly ToolkitRegion _region = new ToolkitRegion{Id = "us-west-2", PartitionId = "aws", DisplayName = "US West (Oregon)"};
       
        public CredentialManagerTest()
        {
            _sharedFactory.Setup(x => x.GetCredentialIdentifiers())
                .Returns(new List<ICredentialIdentifier>());
            _providerFactoryMapping.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, _sharedFactory.Object);
            _identifiers[_sharedIdentifier1.Id] = _sharedIdentifier1;
            _identifiers[_sharedIdentifier2.Id] = _sharedIdentifier2;
            _identifiers[_sdkIdentifier1.Id] = _sdkIdentifier1;
            
            _credentialManager = new CredentialManager(_providerFactoryMapping, _identifiers, _awsCache);
        }

        
        [Fact]
        public void IsLoginRequired()
        {
            var identifier = new SharedCredentialIdentifier("profile");
            _sharedFactory.Setup(x => x.IsLoginRequired(identifier)).Returns(true);
            Assert.True(_credentialManager.IsLoginRequired(identifier));
            _sharedFactory.Verify(x => x.IsLoginRequired(identifier), Times.Once);
        }

        [Fact]
        public void GetCredentialIdentifierById()
        {
            Assert.NotNull(_credentialManager.GetCredentialIdentifierById(_sharedIdentifier1.Id));
        }

        [Fact]
        public void GetCredentialIdentifierById_WhenMissing()
        {
            var identifier = new SharedCredentialIdentifier("testshared3");
            Assert.Null(_credentialManager.GetCredentialIdentifierById(identifier.Id));
        }

        [Fact]
        public void GetCredentialIdentifiers()
        {
            List<string> allIdentifiers =
                new List<string> { "Profile:testshared1", "Profile:testshared2" , "sdk:testsdk1"};
            Assert.Equal(_credentialManager.GetCredentialIdentifiers().Select(x => x.DisplayName).ToList(), allIdentifiers);
        }


        [Fact]
        public void GetAwsCredential_MissingIdentifier()
        {
            var credentialIdentifier = new SharedCredentialIdentifier("testshared3");
            Assert.Throws<CredentialProviderNotFoundException>(() =>
                _credentialManager.GetAwsCredentials(credentialIdentifier, _region));
        }

        [Fact]
        public void GetAwsCredential_MissingFactory()
        {
            Assert.Throws<CredentialProviderNotFoundException>(() =>
                _credentialManager.GetAwsCredentials(_sdkIdentifier1, _region));
        }

        [Fact]
        public void GetAwsCredential_FactoryHasMissingProfile()
        {
            _sharedFactory.Setup(x => x.CreateAwsCredential(_sharedIdentifier1, _region))
                .Throws<InvalidOperationException>();
            Assert.Throws<CredentialProviderNotFoundException>(() =>
                _credentialManager.GetAwsCredentials(_sharedIdentifier1, _region));
            _sharedFactory.Verify(x => x.CreateAwsCredential(_sharedIdentifier1, _region), Times.Once);
        }

        [Fact]
        public void GetAwsCredential()
        {
            _sharedFactory.Setup(x => x.CreateAwsCredential(_sharedIdentifier2, _region))
                .Returns(new BasicAWSCredentials("access", "secret"));
            var awsCredentials = _credentialManager.GetAwsCredentials(_sharedIdentifier2, _region);
            Assert.NotNull(awsCredentials);
            Assert.Equal("access", awsCredentials.GetCredentials().AccessKey);
            Assert.Equal("secret", awsCredentials.GetCredentials().SecretKey);
            _sharedFactory.Verify(x=> x.CreateAwsCredential(_sharedIdentifier2, _region), Times.Once);
        }
    }
}
