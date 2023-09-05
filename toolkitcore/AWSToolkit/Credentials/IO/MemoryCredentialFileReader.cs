using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.IO
{
    internal class MemoryCredentialFileReader : CredentialFileReader
    {
        private readonly MemoryCredentialsFile _file;

        public MemoryCredentialFileReader(MemoryCredentialsFile file)
        {
            _file = file;
        }

        public override void Load()
        {
            ProfileNames = _file.ListProfileNames();
        }

        protected override ICredentialProfileStore GetProfileStore()
        {
            return _file;
        }
    }
}
