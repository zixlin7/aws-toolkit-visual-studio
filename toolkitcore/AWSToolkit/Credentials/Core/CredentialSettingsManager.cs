using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Responsible for managing CRUD operations on credentials
    /// Determines the factory associated with given identifier to actually perform the CRUD operation
    /// </summary>
    public class CredentialSettingsManager : ICredentialSettingsManager
    {
        private readonly Dictionary<string, ICredentialProviderFactory> _factoryMapping =
            new Dictionary<string, ICredentialProviderFactory>();

        public CredentialSettingsManager() : this(new Dictionary<string, ICredentialProviderFactory>())
        {
        }

        public CredentialSettingsManager(Dictionary<string, ICredentialProviderFactory> factoryMapping)
        {
            _factoryMapping = factoryMapping;
        }

        public void CreateProfile(ICredentialIdentifier identifier, ProfileProperties properties)
        {
            GetProfileFactory(identifier).CreateProfile(identifier, properties);
        }

        public void RenameProfile(ICredentialIdentifier oldIdentifier, ICredentialIdentifier newIdentifier)
        {
            if (oldIdentifier.GetType() != newIdentifier.GetType())
            {
                throw new NotSupportedException(
                    $"{oldIdentifier.GetType()} and {newIdentifier.GetType()} are not of the same type. The profile cannot be renamed.");
            }

            GetProfileFactory(oldIdentifier).RenameProfile(oldIdentifier, newIdentifier);
        }

        public void DeleteProfile(ICredentialIdentifier identifier)
        {
            GetProfileFactory(identifier).DeleteProfile(identifier);
        }

        public void UpdateProfile(ICredentialIdentifier identifier,
            ProfileProperties properties)
        {
            GetProfileFactory(identifier).UpdateProfile(identifier, properties);
        }

        public ProfileProperties GetProfileProperties(ICredentialIdentifier identifier)
        {
            return GetProfileFactory(identifier).GetProfileProperties(identifier);
        }

        /// <summary>
        /// Determines and returns the ICredentialProfileProcessor factory associated with the given credential identifier
        /// </summary>
        /// <param name="identifier"></param>
        private ICredentialProfileProcessor GetProfileFactory(ICredentialIdentifier identifier)
        {
            _factoryMapping.TryGetValue(identifier.FactoryId, out var factory);
            if (factory == null)
            {
                throw new ArgumentException(
                    $"Unrecognized provider factory [{identifier.FactoryId}] for the Credential identifier type: {identifier.GetType()}");
            }

            var profileProcessor = factory.GetCredentialProfileProcessor();
            if (profileProcessor == null)
            {
                throw new ArgumentException(
                    $"Provider factory [{identifier.FactoryId}] for the Credential identifier type: {identifier.GetType()} cannot perform CRUD operations.");
            }

            return profileProcessor;
        }
    }
}
