using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Sono
{
    public class SonoProfileProcessorTests
    {
        private static readonly ICredentialIdentifier SampleCredentialsId = FakeCredentialIdentifier.Create("foo");
        private static readonly ProfileProperties SampleProfileProperties = new ProfileProperties();
        private readonly SonoProfileProcessor _sut = new SonoProfileProcessor();

        [Fact]
        public void GetCredentialIdentifiers()
        {
            _sut.CreateProfile(SampleCredentialsId, SampleProfileProperties);

            var credentialsId = Assert.Single(_sut.GetCredentialIdentifiers());
            Assert.Same(SampleCredentialsId, credentialsId);
        }

        [Fact]
        public void GetCredentialIdentifiers_WhenEmpty()
        {
            Assert.Empty(_sut.GetCredentialIdentifiers());
        }

        // CreateProfile is tested through testing GetProfileProperties

        [Fact]
        public void GetProfileProperties()
        {
            _sut.CreateProfile(SampleCredentialsId, SampleProfileProperties);
            Assert.Same(SampleProfileProperties, _sut.GetProfileProperties(SampleCredentialsId));
        }

        [Fact]
        public void GetProfileProperties_UnknownId()
        {
            Assert.Null(_sut.GetProfileProperties(SampleCredentialsId));
        }
    }
}
