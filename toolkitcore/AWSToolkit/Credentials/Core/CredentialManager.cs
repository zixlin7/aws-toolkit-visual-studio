using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    ///  Acts as the entry point to the credential system
    ///  Keeps track of all existing credentials present in the system
    /// </summary>
    public class CredentialManager : ICredentialManager, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CredentialManager));

        // Key: ICredentialIdentifier.Id, Value: ICredentialIdentifier
        private readonly ConcurrentDictionary<string, ICredentialIdentifier> _identifierIds;

        // Key: ICredentialIdentifier.Id, Value: Map of Partition Id to associated Credentials
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ToolkitCredentials>>
            _awsCredentialPartitionCache;
        private readonly Dictionary<string, ICredentialProviderFactory> _providerFactoryMapping;

        /// <summary>
        /// Event to indicate that the credential manager has been updated with the latest list of available credential profiles
        /// </summary>
        public event EventHandler<EventArgs> CredentialManagerUpdated;

        public ICredentialSettingsManager CredentialSettingsManager { get; }

        public CredentialManager(Dictionary<string, ICredentialProviderFactory> factoryMapping) : this(factoryMapping,
            new ConcurrentDictionary<string, ICredentialIdentifier>(),
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ToolkitCredentials>>())
        {
        }

        public CredentialManager(Dictionary<string, ICredentialProviderFactory> providerFactoryMapping,
            ConcurrentDictionary<string, ICredentialIdentifier> identifierIds,
            ConcurrentDictionary<string, ConcurrentDictionary<string, ToolkitCredentials>>
                awsCredentialPartitionCache)
        {
            _providerFactoryMapping = providerFactoryMapping;
            _identifierIds = identifierIds;
            _awsCredentialPartitionCache = awsCredentialPartitionCache;
            LoadIdentifiersAndRegisterFactoryHandlers();
            CredentialSettingsManager = new CredentialSettingsManager(_providerFactoryMapping);
        }

        /// <summary>
        /// Retrieves <see cref="ICredentialIdentifier"/> by id from the dictionary of CredentialIdentifiers
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ICredentialIdentifier GetCredentialIdentifierById(string id)
        {
            _identifierIds.TryGetValue(id, out var identifier);
            return identifier;
        }

        /// <summary>
        /// Retrieves the current list of CredentialIdentifiers 
        /// </summary>
        /// <returns></returns>
        public List<ICredentialIdentifier> GetCredentialIdentifiers()
        {
            var identifiers = _identifierIds.Values.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return identifiers;
        }

        /// <summary>
        ///  Checks if a user login prompt is required for the given <see cref="ICredentialIdentifier"/>.
        /// </summary>
        /// <param name="identifier"></param>
        public bool IsLoginRequired(ICredentialIdentifier identifier)
        {
            _providerFactoryMapping.TryGetValue(identifier.FactoryId, out var factory);
            if (factory == null)
            {
                throw new ArgumentException(
                    $"Unrecognized provider factory [{identifier.FactoryId}] for the Credential identifier type: {identifier.GetType()}");
            }

            return factory.IsLoginRequired(identifier);
        }

        /// <summary>
        /// Checks if the given credential Id supports a connection type
        /// </summary>
        public bool Supports(ICredentialIdentifier credentialIdentifier, AwsConnectionType connectionType)
        {
            _providerFactoryMapping.TryGetValue(credentialIdentifier.FactoryId, out var factory);
            if (factory == null)
            {
                throw new CredentialProviderNotFoundException(
                    $"Unrecognized provider factory [{credentialIdentifier.FactoryId}] for the Credential identifier type: {credentialIdentifier.GetType()}");
            }

            return factory.Supports(credentialIdentifier, connectionType);
        }

        /// <summary>
        /// TODO : Phase out this function in favor of <see cref="GetToolkitCredentials"/>
        /// 
        /// Retrieves an <see cref="AWSCredentials"/> for the specified CredentialIdentifier
        /// <see cref="ICredentialIdentifier"/> and region <see cref="ToolkitRegion"/>
        /// </summary>
        public AWSCredentials GetAwsCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region) =>
            GetToolkitCredentials(credentialIdentifier, region).GetAwsCredentials();

        /// <summary>
        /// Retrieves a <see cref="ToolkitCredentials"/> for the specified CredentialIdentifier
        /// (<see cref="ICredentialIdentifier"/>) and region (<see cref="ToolkitRegion"/>).
        /// </summary>
        public ToolkitCredentials GetToolkitCredentials(ICredentialIdentifier credentialIdentifier,
            ToolkitRegion region)
        {
            //validate that the identifier ID is valid and get latest identifier for it
            _identifierIds.TryGetValue(credentialIdentifier.Id, out var identifier);

            if (identifier == null)
            {
                throw new CredentialProviderNotFoundException(
                    $"Identifier {credentialIdentifier.Id} was not found, can't resolve credentials");
            }

            //check the credential cache if credentials for this partition and identifier ID already exists, else create a new one
            _awsCredentialPartitionCache.TryAdd(identifier.Id, new ConcurrentDictionary<string, ToolkitCredentials>());
            _awsCredentialPartitionCache.TryGetValue(identifier.Id, out var partitionCache);
            if (!partitionCache.TryGetValue(region.PartitionId, out var credentials))
            {
                credentials = CreateToolkitCredentials(identifier, region);
                partitionCache[region.PartitionId] = credentials;
            }

            return partitionCache[region.PartitionId];
        }

        protected void AddIdentifier(ICredentialIdentifier identifier)
        {
            _identifierIds[identifier.Id] = identifier;
        }

        protected void RemoveIdentifier(ICredentialIdentifier identifier)
        {
            _identifierIds.TryRemove(identifier.Id, out _);
            _awsCredentialPartitionCache.TryRemove(identifier.Id, out _);
        }

        protected void ModifyIdentifier(ICredentialIdentifier identifier)
        {
            RemoveIdentifier(identifier);
            _identifierIds[identifier.Id] = identifier;
        }

        private void LoadIdentifiersAndRegisterFactoryHandlers()
        {
            foreach (var credentialProviderFactory in _providerFactoryMapping.Values)
            {
                credentialProviderFactory.GetCredentialIdentifiers().ForEach(AddIdentifier);
                credentialProviderFactory.CredentialsChanged += HandleCredentialChanged;
            }
        }

        /// <summary>
        /// Update the list of CredentialIdentifiers when a CredentialChangeEvent is triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleCredentialChanged(object sender, CredentialChangeEventArgs args)
        {
            args.Added.ForEach(AddIdentifier);
            args.Removed.ForEach(RemoveIdentifier);
            args.Modified.ForEach(ModifyIdentifier);
            CredentialManagerUpdated?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Creates a <see cref= "ToolkitCredentials" /> for the specified CredentialIdentifier
        /// (<see cref="ICredentialIdentifier"/>) and region (<see cref="ToolkitRegion"/>).
        /// </summary>
        private ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier identifier, ToolkitRegion region)
        {
            _providerFactoryMapping.TryGetValue(identifier.FactoryId, out var providerFactory);
            if (providerFactory == null)
            {
                throw new CredentialProviderNotFoundException(
                    $"No provider factory found for identifier type: {identifier.GetType()}");
            }

            try
            {
                return providerFactory.CreateToolkitCredentials(identifier, region);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create ToolkitCredentials for Id: {identifier.Id}: {e.Message}");
                throw new CredentialProviderNotFoundException($"Failed to create Toolkit Credentials: {e.Message}", e);
            }
        }

        public void Dispose()
        {
            foreach (var credentialProviderFactory in _providerFactoryMapping.Values)
            {
                credentialProviderFactory.CredentialsChanged -= HandleCredentialChanged;
            }
        }
    }
}
