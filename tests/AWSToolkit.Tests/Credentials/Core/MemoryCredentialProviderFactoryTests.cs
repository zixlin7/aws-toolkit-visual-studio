using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.IO;

using Moq;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class MemoryCredentialProviderFactoryTests : BaseFileCredentialProviderFactoryTests
    {
        private readonly MemoryCredentialProviderFactory _sut;

        public MemoryCredentialProviderFactoryTests() : base()
        {
            _sut = new MemoryCredentialProviderFactory(
                ProfileHolder.Object,
                new Mock<ICredentialFileReader>().Object,
                new Mock<ICredentialFileWriter>().Object,
                ToolkitShell.Object);
        }

        protected override ProfileCredentialProviderFactory GetFactory()
        {
            return _sut;
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(string profileName)
        {
            return new MemoryCredentialIdentifier(profileName);
        }
    }
}
