using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.IO;
using Moq;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class SDKCredentialProviderFactoryTests : BaseFileCredentialProviderFactoryTests
    {
        private readonly SDKCredentialProviderFactory _factory;

        public SDKCredentialProviderFactoryTests() : base()
        {
            Mock<ICredentialFileReader> reader = new Mock<ICredentialFileReader>();
            Mock<ICredentialFileWriter> writer = new Mock<ICredentialFileWriter>();
            _factory = new SDKCredentialProviderFactory(ProfileHolder.Object, reader.Object, writer.Object,
                ToolkitShell.Object);
        }

        protected override ProfileCredentialProviderFactory GetFactory()
        {
            return _factory;
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(string profileName)
        {
            return new SDKCredentialIdentifier(profileName);
        }
    }
}
