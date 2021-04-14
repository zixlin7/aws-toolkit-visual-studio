using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.IO;
using Moq;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class SharedCredentialProviderFactoryTests : BaseFileCredentialProviderFactoryTests
    {
        private readonly SharedCredentialProviderFactory _factory;

        public SharedCredentialProviderFactoryTests() : base()
        {
            Mock<ICredentialFileReader> reader = new Mock<ICredentialFileReader>();
            Mock<ICredentialFileWriter> writer = new Mock<ICredentialFileWriter>();
            _factory = new SharedCredentialProviderFactory(ProfileHolder.Object, reader.Object, writer.Object,
                ToolkitShell.Object);
        }

        protected override ProfileCredentialProviderFactory GetFactory()
        {
            return _factory;
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(string profileName)
        {
            return new SharedCredentialIdentifier(profileName);
        }
    }
}
