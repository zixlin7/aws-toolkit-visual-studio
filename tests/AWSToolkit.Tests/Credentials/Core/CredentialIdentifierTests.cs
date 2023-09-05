using Amazon.AWSToolkit.Credentials.Core;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class CredentialIdentifierTests
    {
        [Fact]
        public void CredentialIdentifiersThatMatchExactlyAreEqual()
        {
            var expected = new MemoryCredentialIdentifier("Same");
            var actual = new MemoryCredentialIdentifier("Same");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CredentialIdentifiersThatDoNotMatchExactlyAreNotEqual()
        {
            var expected = new MemoryCredentialIdentifier("Same");
            var actual = new MemoryCredentialIdentifier("Different");

            Assert.NotEqual(expected, actual);
        }

        [Fact]
        public void CredentialIdentifiersThatMatchExactlyExceptForTypeAreNotEqual()
        {
            var expected = new MemoryCredentialIdentifier("Same");
            var actual = new SharedCredentialIdentifier("Same");

            Assert.NotEqual<ICredentialIdentifier>(expected, actual);
        }
    }
}
