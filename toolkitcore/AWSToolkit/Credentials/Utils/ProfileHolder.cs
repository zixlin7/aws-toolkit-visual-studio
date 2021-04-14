using System.Collections.Generic;
using System.Linq;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public class ProfileHolder :IProfileHolder
    {
        private readonly Dictionary<string, CredentialProfile> _profiles;

        public ProfileHolder() : this(null)
        {
        }

        public ProfileHolder(Dictionary<string, CredentialProfile> profile)
        {
            _profiles = profile ?? new Dictionary<string, CredentialProfile>();
        }

        public void UpdateProfiles(Dictionary<string, CredentialProfile> profiles)
        {
            _profiles.Clear();
            profiles.ToList().ForEach(pair => _profiles[pair.Key] = pair.Value);
        }

        public CredentialProfile GetProfile(string name)
        {
            if (_profiles.TryGetValue(name, out var profile))
            {
                return profile;
            }

            return null;
        }

        public Dictionary<string, CredentialProfile> GetCurrentProfiles()
        {
            return _profiles;
        }
    }
}
