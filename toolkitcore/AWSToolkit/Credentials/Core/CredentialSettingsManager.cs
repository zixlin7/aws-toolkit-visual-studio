using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Events;

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
            GetProfileProcessor(identifier).CreateProfile(identifier, properties);
        }

        public Task CreateProfileAsync(ICredentialIdentifier credentialIdentifier, ProfileProperties properties, CancellationToken cancellationToken = default)
        {
            var factory = GetProfileFactory(credentialIdentifier);

            return EventWrapperTask.Create<CredentialChangeEventArgs, object>(
                addHandler: handler => factory.CredentialsChanged += handler,
                start: () => CreateProfile(credentialIdentifier, properties),
                handleEvent: (sender, e, setResult) =>
                {
                    if (e.Added.Contains(credentialIdentifier) || e.Modified.Contains(credentialIdentifier))
                    {
                        setResult(null);
                    }
                },
                removeHandler: handler => factory.CredentialsChanged -= handler,
                cancellationToken);
        }

        public void RenameProfile(ICredentialIdentifier oldIdentifier, ICredentialIdentifier newIdentifier)
        {
            if (oldIdentifier.GetType() != newIdentifier.GetType())
            {
                throw new NotSupportedException(
                    $"{oldIdentifier.GetType()} and {newIdentifier.GetType()} are not of the same type. The profile cannot be renamed.");
            }

            GetProfileProcessor(oldIdentifier).RenameProfile(oldIdentifier, newIdentifier);
        }

        public void DeleteProfile(ICredentialIdentifier identifier)
        {
            GetProfileProcessor(identifier).DeleteProfile(identifier);
        }

        public void UpdateProfile(ICredentialIdentifier identifier,
            ProfileProperties properties)
        {
            GetProfileProcessor(identifier).UpdateProfile(identifier, properties);
        }

        public ProfileProperties GetProfileProperties(ICredentialIdentifier identifier)
        {
            return GetProfileProcessor(identifier).GetProfileProperties(identifier);
        }

        private ICredentialProviderFactory GetProfileFactory(ICredentialIdentifier identifier)
        {
            _factoryMapping.TryGetValue(identifier.FactoryId, out var factory);
            if (factory == null)
            {
                throw new ArgumentException(
                    $"Unrecognized provider factory [{identifier.FactoryId}] for the Credential identifier type: {identifier.GetType()}");
            }

            return factory;
        }

        /// <summary>
        /// Determines and returns the ICredentialProfileProcessor factory associated with the given credential identifier
        /// </summary>
        /// <param name="identifier"></param>
        private ICredentialProfileProcessor GetProfileProcessor(ICredentialIdentifier identifier)
        {
            var profileProcessor = GetProfileFactory(identifier).GetCredentialProfileProcessor();
            if (profileProcessor == null)
            {
                throw new ArgumentException(
                    $"Provider factory [{identifier.FactoryId}] for the Credential identifier type: {identifier.GetType()} cannot perform CRUD operations.");
            }

            return profileProcessor;
        }
    }
}
