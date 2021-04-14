using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public abstract class CredentialFileReader : ICredentialFileReader
    {
        public List<string> ProfileNames { get; set; }
        public abstract void Load();
        public abstract CredentialProfileOptions GetCredentialProfileOptions(string profileName);

        public CredentialProfile GetCredentialProfile(string profileName)
        {
            if (GetProfileStore().TryGetProfile(profileName, out CredentialProfile profile) && profile.CanCreateAWSCredentials)
            {
                return profile;
            }

            return null;
        }

        protected abstract ICredentialProfileStore GetProfileStore();
    }
}
