using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public abstract class CredentialFileReader : ICredentialFileReader
    {
        public IEnumerable<string> ProfileNames { get; protected set; }

        public abstract void Load();

        public virtual CredentialProfileOptions GetCredentialProfileOptions(string profileName)
        {
            return GetCredentialProfile(profileName)?.Options;
        }

        public CredentialProfile GetCredentialProfile(string profileName)
        {
            return GetProfileStore().TryGetProfile(profileName, out var profile) && profile.CanCreateAWSCredentials ? profile : null;
        }

        protected abstract ICredentialProfileStore GetProfileStore();
    }
}
