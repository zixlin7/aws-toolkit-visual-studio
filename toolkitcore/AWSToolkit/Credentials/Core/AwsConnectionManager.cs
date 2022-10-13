using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public class AwsConnectionManager : IAwsConnectionManager, IDisposable
    {
        public event EventHandler<ConnectionSettingsChangeArgs> ConnectionSettingsChanged;
        public event EventHandler<ConnectionStateChangeArgs> ConnectionStateChanged;
        public delegate AmazonSecurityTokenServiceClient GetStsClient(AWSCredentials awsCredentials, RegionEndpoint regionEndpoint);

        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(AwsConnectionManager));
        private static readonly RegionEndpoint DefaultSTSClientRegion = RegionEndpoint.USEast1;

        private readonly ITelemetryLogger _telemetryLogger;
        private readonly IRegionProvider _regionProvider;
        private readonly DebounceDispatcher _connectionChangedDispatcher;
        private readonly GetStsClient _getStsClient;
        private readonly MruList<string> _recentCredentialIds = new MruList<string>(MaxMruLimit);
        private readonly MruList<string> _recentRegions = new MruList<string>(MaxMruLimit);
        private readonly object _tokenCancellationSyncLock = new object();
        
        private const double ConnectionChangeDebounceInterval = 1000;
        private const int MaxMruLimit = 5;
        
        private List<ICredentialProviderFactory> _credentialFactories = new List<ICredentialProviderFactory>();
        private ToolkitRegion _activeRegion;
        private ICredentialIdentifier _activeCredentialIdentifier;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ConnectionState _connectionState;
        private readonly IToolkitSettingsRepository _toolkitSettingsRepository;

        public string ActiveAccountId { get; private set; }
        public string ActiveAwsId { get; private set; }
        public ICredentialManager CredentialManager { get; }
        public ToolkitCredentials ActiveCredentials { get; private set; }

        public ToolkitRegion ActiveRegion
        {
            get => _activeRegion;
            private set
            {
                if (value != _activeRegion)
                {
                    _activeRegion = value;
                    if (value != null)
                    {
                        _recentRegions.Add(value.Id);
                    }
                }
            }
        }

        public ICredentialIdentifier ActiveCredentialIdentifier
        {
            get => _activeCredentialIdentifier;
            private set
            {
                if (value != _activeCredentialIdentifier)
                {
                    _activeCredentialIdentifier = value;
                    if (value != null)
                    {
                        _recentCredentialIds.Add(value.Id);
                    }
                }
            }
        }

        public ConnectionState ConnectionState
        {
            get => _connectionState;
            private set
            {
                if (value != _connectionState)
                {
                    _connectionState = value;
                    RaiseConnectionStateChanged(new ConnectionStateChangeArgs { State = _connectionState });
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fnStsClient">Factory method used to produce STS Clients</param>
        /// <param name="credentialManager">Credential retrieval</param>
        /// <param name="regionProvider">Region resolution</param>
        /// <param name="telemetryLogger">Metrics logging</param>
        /// <param name="toolkitSettingsRepository">Repository for getting ToolkitSettings</param>
        public AwsConnectionManager(GetStsClient fnStsClient,
            ICredentialManager credentialManager,
            ITelemetryLogger telemetryLogger,
            IRegionProvider regionProvider,
            IToolkitSettingsRepository toolkitSettingsRepository)
        {
            _telemetryLogger = telemetryLogger;
            _connectionChangedDispatcher = new DebounceDispatcher();
            _getStsClient = fnStsClient;
            CredentialManager = credentialManager;
            _regionProvider = regionProvider;
            _toolkitSettingsRepository = toolkitSettingsRepository;
        }

        /// <summary>
        /// Initializes and loads default connection state
        /// </summary>
        public void Initialize(List<ICredentialProviderFactory> factories)
        {
            _credentialFactories = factories;
            RegisterHandlers();
            ConnectionState = new ConnectionState.InitializingToolkit();

            //load all initial state on background thread to avoid blocking
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    var credentials = ResolveInitialCredentials();
                    var region = ResolveInitialRegion(credentials);
                    ChangeConnectionSettings(credentials, region);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Error setting up initial credential selection. The Toolkit might not auto-select the previously used credentials.", e);
                }
            });
        }

        /// <summary>
        /// Checks if connection state is valid or not 
        /// </summary>
        /// <returns></returns>
        public bool IsValidConnectionSettings()
        {
            return ConnectionState.IsValid(ConnectionState);
        }

        /// <summary>
        /// Re-triggers validation of current connection
        /// </summary>
        public void RefreshConnectionState()
        {
            UpdateConnectionAndNotify(ActiveCredentialIdentifier, true, ActiveRegion, true);
        }

        /// <summary>
        /// Changes the credentials and then validates them. Notifies subscribers of results
        /// </summary>
        /// <param name="identifier"></param>
        public void ChangeCredentialProvider(ICredentialIdentifier identifier)
        {
            if (ActiveCredentialIdentifier != identifier)
            {
                UpdateConnectionAndNotify(identifier, true, ActiveRegion, false);
            }
        }

        /// <summary>
        /// Changes the region and then validates them. Notifies subscribers of results
        /// </summary>
        /// <param name="region"></param>
        public void ChangeRegion(ToolkitRegion region)
        {
            if (ActiveRegion != region)
            {
                UpdateConnectionAndNotify(ActiveCredentialIdentifier, false, region, true);
            }
        }

        /// <summary>
        /// Returns the list of recently used <see cref="ICredentialIdentifier"/>
        /// </summary>
        /// <returns></returns>
        public List<ICredentialIdentifier> GetRecentCredentialIdentifiers()
        {
            return _recentCredentialIds.Select(x => CredentialManager.GetCredentialIdentifierById(x))
                .Where(y => y != null).ToList();
        }

        /// <summary>
        /// Returns the list of recently used <see cref="ToolkitRegion"/>
        /// </summary>
        /// <returns></returns>
        public List<ToolkitRegion> GetRecentRegions()
        {
           return _recentRegions
                .Select(x => _regionProvider.GetRegion(x)).Where(y => y != null).ToList();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            foreach (var factory in _credentialFactories)
            {
                factory.CredentialsChanged -= HandleCredentialChanged;
            }
        }

        public void ChangeConnectionSettings(ICredentialIdentifier identifier, ToolkitRegion region)
        {
            UpdateConnectionAndNotify(identifier, true, region, true);
        }

        public static AmazonSecurityTokenServiceClient DefaultStsClientCreator(
            AWSCredentials credentials,
            RegionEndpoint endpoint)
        {
            return ServiceClientCreator.CreateServiceClient(
                typeof(AmazonSecurityTokenServiceClient),
                credentials, endpoint) as AmazonSecurityTokenServiceClient;
        }

        private void HandleCredentialChanged(object sender, CredentialChangeEventArgs args)
        {
            if (ActiveCredentialIdentifier != null)
            {
                var removedIdentifier = args.Removed.Any(x => x.Id.Equals(ActiveCredentialIdentifier.Id));
                var modifiedIdentifier = args.Modified.Any(x => x.Id.Equals(ActiveCredentialIdentifier.Id));
                if (removedIdentifier)
                {
                    ChangeCredentialProvider(null);
                }
                else if (modifiedIdentifier)
                {
                    RefreshConnectionState();
                }
            }
        }

        /// <summary>
        /// Updates connection state based on current connection settings and notifies user
        /// </summary>
        private void UpdateConnectionAndNotify(ICredentialIdentifier identifier, bool updateId, ToolkitRegion region, bool updateRegion)
        {
            lock (_tokenCancellationSyncLock)
            {
                // if an update job is already in progress, cancel it
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                var oldRegion = ActiveRegion;
                var oldCredentialId = ActiveCredentialIdentifier;

                var isInitial = ConnectionState is ConnectionState.InitializingToolkit;
                ConnectionState = new ConnectionState.ValidatingConnection();

                if (updateId)
                {
                    ActiveCredentialIdentifier = identifier;
                }

                if (updateRegion)
                {
                    ActiveRegion = region;
                }

                // Raise connection settings changed if identifier or region changes
                if (oldCredentialId?.Id != identifier?.Id || oldRegion?.Id != region?.Id)
                {
                    RaiseConnectionSettingChanged(new ConnectionSettingsChangeArgs { CredentialIdentifier = identifier, Region = region });
                }
            
                // debounce to handle consecutive settings change
                _connectionChangedDispatcher.Debounce(ConnectionChangeDebounceInterval,
                    _ => UpdateConnectionSettings(isInitial));
            }
        }

        private void UpdateConnectionSettings(bool isInitial)
        {
            var identifier = ActiveCredentialIdentifier;
            var region = ActiveRegion;
            if (identifier == null || region == null)
            {
                ConnectionState = new ConnectionState.IncompleteConfiguration(identifier, region);
                return;
            }

            //check if user login needed
            if (isInitial && CredentialManager.IsLoginRequired(identifier))
            {
                //specify if login is mfa or sso in the state message
                ConnectionState = new ConnectionState.UserAction("AWS login needed");
                return;
            }

            lock (_tokenCancellationSyncLock)
            {
                _cancellationTokenSource = new CancellationTokenSource();

                Task.Run(async () => { await PerformValidation(identifier, region, _cancellationTokenSource.Token); },
                    _cancellationTokenSource.Token).LogExceptionAndForget();
            }
        }
        /// <summary>
        /// Performs validation and updates connection state
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="region"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task PerformValidation(ICredentialIdentifier identifier, ToolkitRegion region,
            CancellationToken token)
        {
            var validationResult = Result.Failed;
            string accountId = string.Empty;
            string awsId = string.Empty;

            try
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                var credentials = CredentialManager.GetToolkitCredentials(identifier, region);

                if (credentials.Supports(AwsConnectionType.AwsCredentials))
                {
                    accountId = await GetAccountId(credentials.GetAwsCredentials(), region, token);
                }

                if (credentials.Supports(AwsConnectionType.AwsToken))
                {
                    awsId = await GetAwsId(credentials, region, token);
                }

                var connectionState = new ConnectionState.ValidConnection(identifier, region);

                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                SetValidationResults(identifier, region, connectionState, credentials, accountId, awsId, token);

                validationResult = Result.Succeeded;
            }
            catch (OperationCanceledException e)
            {
                validationResult = Result.Cancelled;
                LOGGER.Error($"Failed to switch to credentials: {identifier?.DisplayName}", e);
                var connectionState = new ConnectionState.InvalidConnection("New connection settings chosen");
                SetValidationResults(identifier, region, connectionState, null, string.Empty, string.Empty, token);
            }
            catch (Exception e)
            {
                validationResult = Result.Failed;
                LOGGER.Error($"Failed to switch to credentials: {identifier?.DisplayName}", e);
                var connectionState = new ConnectionState.InvalidConnection(e.Message);
                SetValidationResults(identifier, region, connectionState, null, string.Empty, string.Empty, token);
            }
            finally
            {
                _telemetryLogger.RecordAwsValidateCredentials(new AwsValidateCredentials()
                {
                    AwsAccount = string.IsNullOrEmpty(accountId) ? MetadataValue.NotSet : accountId,
                    AwsRegion = region.Id,
                    Result = validationResult,
                    CredentialType = CredentialManager.CredentialSettingsManager
                        .GetCredentialType(identifier)
                        .AsTelemetryCredentialType(),
                });
            }
        }

        /// <summary>
        /// Applies Validation results to the connection manager if the token has not been cancelled and
        /// if the state currently represents the provided Id + Region.
        /// </summary>
        private void SetValidationResults(ICredentialIdentifier credentialsId, ToolkitRegion region,
            ConnectionState connectionState, ToolkitCredentials credentials, string accountId, string awsId, CancellationToken token)
        {
            if (!token.IsCancellationRequested && ActiveCredentialIdentifier == credentialsId && ActiveRegion == region)
            {
                ActiveAccountId = accountId;
                ActiveCredentials = credentials;
                ConnectionState = connectionState;
            }
        }

        /// <summary>
        /// Performs the deep validation check by making a call to sts:GetCallerIdentity
        /// and returns the account id retrieved from it
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="region"></param>
        private async Task<string> GetAccountId(AWSCredentials credentials, ToolkitRegion region, CancellationToken token)
        {
            if (_regionProvider.IsRegionLocal(region.Id))
            {
                return string.Empty;
            }   
            var stsRegion = string.IsNullOrEmpty(region.Id)
                ? DefaultSTSClientRegion
                : RegionEndpoint.GetBySystemName(region.Id);
            var stsClient = _getStsClient(credentials, stsRegion);
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest(), token);
            return response.Account;
        }

        /// <summary>
        /// Token based credentials approach for looking up the AWS ID.
        /// </summary>
        private async Task<string> GetAwsId(ToolkitCredentials credentials, ToolkitRegion region,
            CancellationToken token)
        {
            if (_regionProvider.IsRegionLocal(region.Id))
            {
                return string.Empty;
            }

            // TODO : Get client for looking up AwsId
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            // TODO : request AwsId
            return string.Empty;
        }

        private void RegisterHandlers()
        {
            foreach (var factory in _credentialFactories)
            {
                factory.CredentialsChanged += HandleCredentialChanged;
            }
        }

        private ICredentialIdentifier ResolveInitialCredentials()
        {
            var credentialIdentifiers = CredentialManager.GetCredentialIdentifiers();
            if (credentialIdentifiers.Count <= 0)
            {
                return null;
            }

            //look for previously selected credential id first
            var lastSelectedCredentialId =  _toolkitSettingsRepository.GetLastSelectedCredentialId();
            if (!string.IsNullOrWhiteSpace(lastSelectedCredentialId))
            {
                var identifier = CredentialManager.GetCredentialIdentifierById(lastSelectedCredentialId);
                if (identifier != null)
                {
                    return identifier;
                }
            }

            var defaultSdkCredentialIdentifier = new SDKCredentialIdentifier("default");
            var defaultSharedCredentialIdentifier = new SharedCredentialIdentifier("default");

            var defaultIdentifierPrefixes = new List<string>
            {
                defaultSdkCredentialIdentifier.Id,
                defaultSharedCredentialIdentifier.Id,
                SDKCredentialProviderFactory.SdkProfileFactoryId,
                SharedCredentialProviderFactory.SharedProfileFactoryId
            };

            foreach (var prefix in defaultIdentifierPrefixes)
            {
                var identifier = credentialIdentifiers.FirstOrDefault(x => x.Id.StartsWith(prefix));
                if (identifier != null)
                {
                    return identifier;
                }
            }

            return null;
        }

        private ToolkitRegion ResolveInitialRegion(ICredentialIdentifier identifier)
        {
            if (_regionProvider == null)
            {
                return null;
            }
          
            var candidateRegionIds = new List<string>
            {
                _toolkitSettingsRepository.GetLastSelectedRegion()
            };

            if (identifier != null)
            {
                candidateRegionIds.Add(CredentialManager.CredentialSettingsManager?.GetProfileProperties(identifier).Region);
            }
            candidateRegionIds.Add(RegionEndpoint.USEast1.SystemName);

            foreach (var prefix in candidateRegionIds)
            {
                var region = _regionProvider.GetRegion(prefix);
                if (region != null)
                {
                    return region;
                }
            }

            return null;
        }

        private void RaiseConnectionStateChanged(ConnectionStateChangeArgs args)
        {
            ConnectionStateChanged?.Invoke(this, args);
        }

        private void RaiseConnectionSettingChanged(ConnectionSettingsChangeArgs args)
        {
            ConnectionSettingsChanged?.Invoke(this, args);
        }
    }
}
