using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Events;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class AwsConnectionManagerTest
    {
        private const string SampleAccountId = "11222333444";
        private const string SampleAwsId = "aws-id";
        private const int cancellationDelayMs = 5000;

        // Cause tests to exit "soon" if state doesn't go as expected
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource(cancellationDelayMs);
        private readonly CancellationToken _cancellationToken;

        private readonly Mock<ITelemetryLogger> _telemetryLogger = new Mock<ITelemetryLogger>();
        private readonly Mock<ICredentialManager> _credentialManager = new Mock<ICredentialManager>();

        private readonly Mock<IRegionProvider> _regionProvider = new Mock<IRegionProvider>();

        private readonly Mock<ICredentialProviderFactory> _sharedFactory = new Mock<ICredentialProviderFactory>();
        private readonly FakeAwsTokenProvider _tokenProvider = new FakeAwsTokenProvider();

        private readonly Mock<ICredentialSettingsManager> _credentialSettingsManager =
            new Mock<ICredentialSettingsManager>();

        private readonly Dictionary<string, ICredentialProviderFactory> _factoryMap =
            new Dictionary<string, ICredentialProviderFactory>();

        private readonly Dictionary<string, ToolkitRegion> _regions =
            new Dictionary<string, ToolkitRegion>();

        private readonly Dictionary<string, ICredentialIdentifier> _identifiers =
            new Dictionary<string, ICredentialIdentifier>();

        private readonly List<string> _availableCredentials = new List<string>();

        private readonly ICredentialIdentifier _defaultSampleIdentifier = new SharedCredentialIdentifier("default");
        private readonly ICredentialIdentifier _sampleIdentifier = new SharedCredentialIdentifier("sampleMock");
        private readonly ICredentialIdentifier _sampleIdentifier2 = new SDKCredentialIdentifier("sampleMock2");
        private readonly ICredentialIdentifier _sampleTokenBasedIdentifier = FakeCredentialIdentifier.Create("token-cred-id");
        private readonly ProfileProperties _sampleCodeCatalystScopedProfileProperties = new ProfileProperties()
        {
            SsoRegistrationScopes = SonoProperties.CodeCatalystScopes.ToArray(),
        };
        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion{Id = "region1", PartitionId = "aws", DisplayName = "SampleRegion1"};
        private readonly ToolkitRegion _sampleRegion2 = new ToolkitRegion{Id = "region2", PartitionId = "aws", DisplayName = "SampleRegion2"};
        private readonly ToolkitRegion _sampleLocalRegion = new ToolkitRegion { Id = $"{ RegionProvider.LocalRegionIdPrefix }region", PartitionId = "aws", DisplayName = "SampleLocalRegion" };

        private readonly ToolkitRegion _defaultToolkitRegion = new ToolkitRegion
        {
            Id = "DefaultRegion1", DisplayName = "default-Region", PartitionId = PartitionIds.DefaultPartitionId
        };

        private readonly FakeIdentityResolver _identityResolver = new FakeIdentityResolver();
        private readonly AwsConnectionManager _awsConnectionManager;

        public AwsConnectionManagerTest()
        {
            _cancellationToken = _cancellationTokenSource.Token;
            PopulateIdentifiers();
            PopulateRegions();

            var properties = new ProfileProperties { Region = _defaultToolkitRegion.Id };
            RegisterProfileProperties(_defaultSampleIdentifier, properties);

            _identityResolver.AccountId = SampleAccountId;

            _credentialManager.SetupGet(x => x.CredentialSettingsManager).Returns(_credentialSettingsManager.Object);

            _regionProvider.Setup(x => x.GetRegion(It.IsAny<string>()))
                .Returns<string>(regionId =>
                {
                    if (string.Equals(regionId, RegionEndpoint.USEast1.SystemName))
                    {
                        return _defaultToolkitRegion;
                    }
                    if (string.IsNullOrWhiteSpace(regionId) || !_regions.ContainsKey(regionId))
                    {
                        return null;
                    }

                    return _regions[regionId];
                });
            _regionProvider.Setup(x => x.IsRegionLocal(_sampleLocalRegion.Id)).Returns(true);
            
            _credentialManager.Setup(x => x.GetCredentialIdentifiers())
                .Returns(() => _identifiers.Values.ToList());
            _credentialManager.Setup(x => x.GetCredentialIdentifierById(It.IsAny<string>()))
                .Returns<string>(identifierId =>
                {
                    if (!_availableCredentials.Contains(identifierId) || !_identifiers.ContainsKey(identifierId))
                    {
                        return null;
                    }

                    return _identifiers[identifierId];
                });
            _credentialManager.Setup(x =>
                    x.GetToolkitCredentials(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns<ICredentialIdentifier, ToolkitRegion>((credentialIdentifier, _) =>
                {
                    if (credentialIdentifier == _sampleTokenBasedIdentifier)
                    {
                        return new ToolkitCredentials(credentialIdentifier, _tokenProvider);
                    }

                    return new ToolkitCredentials(credentialIdentifier, new AnonymousAWSCredentials());
                });

            _awsConnectionManager = CreateAwsConnectionManager();
        }

        private AwsConnectionManager CreateAwsConnectionManager()
        {
            return new AwsConnectionManager(_identityResolver, _credentialManager.Object, _telemetryLogger.Object,
                _regionProvider.Object, new NoopToolkitSettingsRepository());
        }

        [Fact]
        public async Task IncompleteConfiguration_WhenInitiallyLoaded()
        {
            _credentialManager.Setup(x => x.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());

            await WaitForConnectionStateAsync(typeof(ConnectionState.IncompleteConfiguration),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.False(_awsConnectionManager.IsValidConnectionSettings());
            Assert.IsType<ConnectionState.IncompleteConfiguration>(_awsConnectionManager.ConnectionState);
        }

        [Fact]
        public async Task DefaultSettingsChosen_WhenInitiallyLoaded()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.Equal(_defaultSampleIdentifier.Id,
                _awsConnectionManager.ActiveCredentialIdentifier.Id);
            Assert.Equal(_defaultToolkitRegion.Id, _awsConnectionManager.ActiveRegion.Id);
        }

        [Fact]
        public async Task CredentialRegionChosen_WhenInitiallyLoaded()
        {
            // arrange.
            _availableCredentials.Add(_defaultSampleIdentifier.Id);

            // act.
            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(new List<ICredentialProviderFactory>()),
                _cancellationToken);

            // assert.
            Assert.Equal(_defaultSampleIdentifier.Id, _awsConnectionManager.ActiveCredentialIdentifier.Id);
            Assert.Equal(_defaultToolkitRegion.Id, _awsConnectionManager.ActiveRegion.Id);
        }

        [Fact]
        public async Task UserActionConnectionState_WhenLoaded()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            RegisterProfileProperties(_defaultSampleIdentifier, new ProfileProperties()
            {
                Region = _sampleRegion.Id,
            });
            _credentialManager.Setup(x => x.IsLoginRequired(_defaultSampleIdentifier)).Returns(true);

            await WaitForConnectionStateAsync(typeof(ConnectionState.UserAction),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.False(_awsConnectionManager.IsValidConnectionSettings());
            Assert.IsType<ConnectionState.UserAction>(_awsConnectionManager.ConnectionState);
            Assert.Equal(_defaultSampleIdentifier.Id,
                _awsConnectionManager.ActiveCredentialIdentifier.Id);
        }

        [Fact]
        public async Task ChangeConnectionSettingsShouldUpdateCredentialIdentifierAndRegion()
        {
            _identifiers.Clear();
            await WaitForConnectionStateAsync(typeof(ConnectionState.IncompleteConfiguration),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            _availableCredentials.Add(_sampleIdentifier.Id);
            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeConnectionSettings(_sampleIdentifier, _sampleRegion),
                _cancellationToken);

            Assert.Equal(_sampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
            Assert.Equal(_sampleRegion, _awsConnectionManager.ActiveRegion);
        }

        [Fact]
        public async Task ChangeConnectionSettingsShouldUpdateCredentialIdentifierAndRegionAsync()
        {
            _identifiers.Clear();
            await WaitForConnectionStateAsync(typeof(ConnectionState.IncompleteConfiguration),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            _availableCredentials.Add(_sampleIdentifier.Id);
            var connectionState = await _awsConnectionManager.ChangeConnectionSettingsAsync(_sampleIdentifier, _sampleRegion);

            Assert.IsType<ConnectionState.ValidConnection>(connectionState);
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
            Assert.Equal(_sampleRegion, _awsConnectionManager.ActiveRegion);
        }

        [Fact]
        public async Task ChangeCredentialProviderShouldUpdateAccountId()
        {
            _identifiers.Clear();
            await WaitForConnectionStateAsync(typeof(ConnectionState.IncompleteConfiguration),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            _availableCredentials.Add(_sampleIdentifier.Id);
            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier),
                _cancellationToken);

            Assert.Equal(SampleAccountId, _awsConnectionManager.ActiveAccountId);
            Assert.True(string.IsNullOrEmpty(_awsConnectionManager.ActiveAwsId));
        }

        [Fact]
        public async Task ChangeCredentialProviderShouldUpdateAwsId()
        {
            _identityResolver.AwsId = SampleAwsId;
            _identifiers.Clear();

            await WaitForConnectionStateAsync(typeof(ConnectionState.IncompleteConfiguration),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            _availableCredentials.Add(_sampleTokenBasedIdentifier.Id);
            RegisterProfileProperties(_sampleTokenBasedIdentifier, _sampleCodeCatalystScopedProfileProperties);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleTokenBasedIdentifier),
                _cancellationToken);

            Assert.Equal(SampleAwsId, _awsConnectionManager.ActiveAwsId);
            Assert.True(string.IsNullOrEmpty(_awsConnectionManager.ActiveAccountId));
        }

        [Fact]
        public async Task ActiveCredentialsChosen_WhenCredentialSpecified()
        {
            _availableCredentials.Add(_sampleIdentifier.Id);
            _availableCredentials.Add(_sampleIdentifier2.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier),
                _cancellationToken);

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.GetRecentCredentialIdentifiers()[0]);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier2),
                _cancellationToken);

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());

            var credentialIdsMru = _awsConnectionManager.GetRecentCredentialIdentifiers();
            Assert.Equal(_sampleIdentifier2, credentialIdsMru[0]);
            Assert.Equal(_sampleIdentifier, credentialIdsMru[1]);
        }

        [Fact]
        public async Task ActiveRegionChosen_WhenRegionSpecified()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.Equal(_defaultToolkitRegion.DisplayName, _awsConnectionManager.ActiveRegion.DisplayName);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeRegion(_sampleRegion),
                _cancellationToken);

            Assert.Equal(_sampleRegion, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleRegion.Id, _awsConnectionManager.GetRecentRegions()[0].Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeRegion(_sampleRegion2),
                _cancellationToken);

            Assert.Equal(_sampleRegion2, _awsConnectionManager.ActiveRegion);

            var regionsMru = _awsConnectionManager.GetRecentRegions();
            Assert.Equal(_sampleRegion2, regionsMru[0]); // most recent
            Assert.Equal(_sampleRegion, regionsMru[1]); // previous value
        }

        [Fact]
        public async Task ConnectionStateEventFired_WhenCredentialChanged()
        {
            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidatingConnection),
                () =>
                {
                    var receivedEvent = Assert.Raises<ConnectionStateChangeArgs>(
                        a => _awsConnectionManager.ConnectionStateChanged += a,
                        a => _awsConnectionManager.ConnectionStateChanged -= a,
                        () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier));

                    Assert.NotNull(receivedEvent);
                },
                _cancellationToken);
        }

        [Fact]
        public async Task ConnectionStateEventFired_WhenRegionChanged()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            var receivedEvent = Assert.Raises<ConnectionStateChangeArgs>(
                a => _awsConnectionManager.ConnectionStateChanged += a,
                a => _awsConnectionManager.ConnectionStateChanged -= a,
                () => _awsConnectionManager.ChangeRegion(_sampleRegion));

            Assert.NotNull(receivedEvent);
        }

        [Fact]
        public async Task InvalidConnection_WhenSelectedCredentialRemoved()
        {
            _availableCredentials.Add(_sampleIdentifier.Id);
            _factoryMap.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, _sharedFactory.Object);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier),
                _cancellationToken);

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier.Id, _awsConnectionManager.ActiveCredentialIdentifier.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.IncompleteConfiguration),
                () =>
                {
                    _sharedFactory.Raise(x => x.CredentialsChanged += null,
                        new CredentialChangeEventArgs
                        {
                            Removed = new List<ICredentialIdentifier> { _sampleIdentifier },
                            Modified = new List<ICredentialIdentifier>()
                        });
                },
                _cancellationToken);

            Assert.Null(_awsConnectionManager.ActiveCredentialIdentifier);
        }


        [Fact]
        public async Task ConnectionRefreshed_WhenSelectedCredentialModified()
        {
            _availableCredentials.Add(_sampleIdentifier.Id);
            _factoryMap.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, _sharedFactory.Object);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier),
                _cancellationToken);

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier.Id, _awsConnectionManager.ActiveCredentialIdentifier.Id);

            // System progresses through "Validating" and into "Valid"
            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidatingConnection),
                () =>
                {
                    _sharedFactory.Raise(x => x.CredentialsChanged += null,
                        new CredentialChangeEventArgs
                        {
                            Modified = new List<ICredentialIdentifier> { _sampleIdentifier },
                            Removed = new List<ICredentialIdentifier>()
                        });
                },
                _cancellationToken);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => { },
                _cancellationToken);

            Assert.Equal(_sampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
        }


        [Fact]
        public async Task InvalidConnection_WhenSelectedCredentialFailsValidation()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _identityResolver.GetAccountIdAsyncThrows = true;

            await WaitForConnectionStateAsync(typeof(ConnectionState.InvalidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.Equal(_defaultSampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
        }

        [Fact]
        public async Task UpdateCancelled_WhenSettingsChangedConsecutively()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());

            // System progresses through "Validating" and into "Valid"
            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidatingConnection),
                () =>
                {
                    _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier);
                    _awsConnectionManager.ChangeRegion(_sampleRegion);
                },
                _cancellationToken);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => { },
                _cancellationToken);

            Assert.Equal(_sampleRegion, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);

            var regionsMru = _awsConnectionManager.GetRecentRegions();
            Assert.Equal(_sampleRegion, regionsMru[0]); // most recent
            Assert.Equal(_defaultToolkitRegion, regionsMru[1]); // previous value
        }

        [Fact]
        public async Task AccountIdEmpty_WhenLocalRegionSelected()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.Initialize(_factoryMap.Values.ToList()),
                _cancellationToken);

            Assert.Equal(_defaultToolkitRegion.DisplayName, _awsConnectionManager.ActiveRegion.DisplayName);
            Assert.Equal(SampleAccountId, _awsConnectionManager.ActiveAccountId);

            await WaitForConnectionStateAsync(typeof(ConnectionState.ValidConnection),
                () => _awsConnectionManager.ChangeRegion(_sampleLocalRegion),
                _cancellationToken);

            Assert.Equal(_sampleLocalRegion, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleLocalRegion.Id, _awsConnectionManager.GetRecentRegions()[0].Id);
            Assert.True(string.IsNullOrEmpty(_awsConnectionManager.ActiveAccountId));
        }

        /// <summary>
        /// Perform an operation, then wait to return control to the caller until
        /// ConnectionState changes to the expected state.
        /// The token is expected to be short-lived, which causes this to fail if
        /// the expected state doesn't happen.
        /// </summary>
        private async Task WaitForConnectionStateAsync(Type expectedState, Action operation,
            CancellationToken cancellationToken)
        {
            await EventWrapperTask.Create<ConnectionStateChangeArgs, ConnectionState>(
                addHandler: handler => _awsConnectionManager.ConnectionStateChanged += handler,
                start: operation,
                handleEvent: (sender, e, setResult) =>
                {
                    if (e.State.GetType() == expectedState)
                    {
                        setResult(e.State);
                    }
                },
                removeHandler: handler => _awsConnectionManager.ConnectionStateChanged -= handler,
                cancellationToken);
        }

        private void RegisterProfileProperties(ICredentialIdentifier credentialsId, ProfileProperties properties)
        {
            _credentialSettingsManager.Setup(x => x.GetProfileProperties(credentialsId)).Returns(properties);
        }

        private void PopulateRegions()
        {
            new List<ToolkitRegion>
            {
               _defaultToolkitRegion, _sampleRegion, _sampleRegion2, _sampleLocalRegion
            }.ForEach(region => _regions[region.Id] = region);
        }

        private void PopulateIdentifiers()
        {
            new List<ICredentialIdentifier> {_defaultSampleIdentifier, _sampleIdentifier, _sampleIdentifier2}.ForEach(
                identifier => _identifiers[identifier.Id] = identifier);
        }
    }
}
