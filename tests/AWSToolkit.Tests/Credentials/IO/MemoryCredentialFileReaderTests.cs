using System.Linq;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class MemoryCredentialFileReaderTests
    {
        private readonly MemoryCredentialFileReader _sut;

        private readonly MemoryCredentialsFile _file;

        private readonly CredentialProfile _sampleProfile = CredentialProfileTestHelper.Basic.Valid.AccessAndSecret;

        private readonly CredentialProfile _sampleAlternateProfile = CredentialProfileTestHelper.Basic.Valid.Token;

        public MemoryCredentialFileReaderTests()
        {
            _file = new MemoryCredentialsFile();
            _sut = new MemoryCredentialFileReader(_file);
        }

        [Fact]
        public void EmptyCredentials()
        {
            Assert.Null(_sut.ProfileNames);
            _sut.Load();
            Assert.Empty(_sut.ProfileNames);
            Assert.Null(_sut.GetCredentialProfileOptions(_sampleProfile.Name));
            Assert.Null(_sut.GetCredentialProfile(_sampleProfile.Name));
        }

        [Fact]
        public void ValidCredentials()
        {
            Assert.Null(_sut.ProfileNames);

            _file.RegisterProfile(_sampleProfile);
            _file.RegisterProfile(_sampleAlternateProfile);

            _sut.Load();
            var credentialProfileOptions = _sut.GetCredentialProfileOptions(_sampleProfile.Name);

            Assert.Equal(2, _sut.ProfileNames.Count());
            Assert.NotNull(credentialProfileOptions);
            Assert.NotNull(_sut.GetCredentialProfile(_sampleAlternateProfile.Name));
            Assert.Equal(_sampleProfile.Options.AccessKey,
                credentialProfileOptions.AccessKey);
        }
    }
}
