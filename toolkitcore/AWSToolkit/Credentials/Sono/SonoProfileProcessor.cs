using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.Credentials.Sono
{
    public class SonoProfileProcessor : ICredentialProfileProcessor
    {
        /// <summary>
        /// Indexed by credentialId
        /// </summary>
        private readonly Dictionary<string, ProfileProperties> _sonoProfiles =
            new Dictionary<string, ProfileProperties>();

        private readonly HashSet<ICredentialIdentifier> _credentialIdentifiers = new HashSet<ICredentialIdentifier>();

        public IEnumerable<ICredentialIdentifier> GetCredentialIdentifiers()
        {
            return _credentialIdentifiers;
        }

        public void CreateProfile(ICredentialIdentifier identifier, ProfileProperties properties)
        {
            _credentialIdentifiers.Add(identifier);
            _sonoProfiles[identifier.Id] = properties;
        }

        [Obsolete("Not Implemented - Connections are fixed and cannot be modified", true)]
        public void RenameProfile(ICredentialIdentifier oldIdentifier, ICredentialIdentifier newIdentifier)
        {
            // Sono connections are fixed, and cannot be modified
            throw new NotImplementedException();
        }

        [Obsolete("Not Implemented - Connections are fixed and cannot be modified", true)]
        public void DeleteProfile(ICredentialIdentifier identifier)
        {
            // Sono connections are fixed, and cannot be modified
            throw new NotImplementedException();
        }

        [Obsolete("Not Implemented - Connections are fixed and cannot be modified", true)]
        public void UpdateProfile(ICredentialIdentifier identifier, ProfileProperties properties)
        {
            // Sono connections are fixed, and are not intended to be adjusted by end-users
            throw new NotImplementedException();
        }

        public ProfileProperties GetProfileProperties(ICredentialIdentifier identifier)
        {
            return _sonoProfiles.TryGetValue(identifier.Id, out var profileProperties) ? profileProperties : null;
        }
    }
}
