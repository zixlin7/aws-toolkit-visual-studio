using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Core
{
    public class AwsConnectionManagerTest
    {
        private readonly Mock<ITelemetryLogger> _telemetryLogger = new Mock<ITelemetryLogger>();
        private readonly Mock<ICredentialManager> _credentialManager = new Mock<ICredentialManager>();
        private readonly Mock<AmazonSecurityTokenServiceClient> _stsClient =
            new Mock<AmazonSecurityTokenServiceClient>();

        private readonly Mock<IRegionProvider> _regionProvider = new Mock<IRegionProvider>();

        private readonly Mock<ICredentialProviderFactory> _sharedFactory = new Mock<ICredentialProviderFactory>();

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
        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion{Id = "region1", PartitionId = "aws", DisplayName = "SampleRegion1"};
        private readonly ToolkitRegion _sampleRegion2 = new ToolkitRegion{Id = "region2", PartitionId = "aws", DisplayName = "SampleRegion2"};
        private readonly ToolkitRegion _sampleLocalRegion = new ToolkitRegion { Id = $"{ RegionProvider.LocalRegionIdPrefix }region", PartitionId = "aws", DisplayName = "SampleLocalRegion" };

        private readonly ToolkitRegion _defaultToolkitRegion = new ToolkitRegion
        {
            Id = "DefaultRegion1", DisplayName = "default-Region", PartitionId = PartitionIds.AWS
        };

        private readonly ManualResetEvent _connectionStateIsTerminalEvent = new ManualResetEvent(false);

        private readonly List<ConnectionState> _stateList;
        private readonly AwsConnectionManager.GetStsClient _fnStsClient;
        private AwsConnectionManager _awsConnectionManager;

        public AwsConnectionManagerTest()
        {
            PopulateIdentifiers();
            PopulateRegions();

            _stsClient.Setup(x =>
                    x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCallerIdentityResponse{Account = "11222333444"});

            _fnStsClient = SetupStsClient;
            _credentialManager.Setup(x => x.CredentialSettingsManager).Returns((ICredentialSettingsManager)null);
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
                .Returns(_identifiers.Values.ToList());
            _credentialManager.Setup(x => x.GetCredentialIdentifierById(It.IsAny<string>()))
                .Returns<string>(identifierId =>
                {
                    if (!_availableCredentials.Contains(identifierId) || !_identifiers.ContainsKey(identifierId))
                    {
                        return null;
                    }

                    return _identifiers[identifierId];
                });

            _stateList = new List<ConnectionState>();
        }

        [Fact]
        public void IncompleteConfiguration_WhenInitiallyLoaded()
        {
            _credentialManager.Setup(x => x.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());
            _awsConnectionManager = new AwsConnectionManager( _fnStsClient, _credentialManager.Object,
                _telemetryLogger.Object,
                _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());

            WaitUntilConnectionStateIsStable();

            Assert.False(_awsConnectionManager.IsValidConnectionSettings());
            Assert.IsType<ConnectionState.IncompleteConfiguration>(_awsConnectionManager.ConnectionState);
        }

        [Fact]
        public void DefaultSettingsChosen_WhenInitiallyLoaded()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            Assert.Equal(_defaultSampleIdentifier.Id,
                _awsConnectionManager.ActiveCredentialIdentifier.Id);
            Assert.Equal(_defaultToolkitRegion.Id, _awsConnectionManager.ActiveRegion.Id);
        }

        [Fact]
        public void CredentialRegionChosen_WhenInitiallyLoaded()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            var properties = new ProfileProperties { Region = _sampleRegion.Id };
            _credentialSettingsManager.Setup(x => x.GetProfileProperties(_defaultSampleIdentifier))
                .Returns(properties);
            _credentialManager.Setup(x => x.CredentialSettingsManager).Returns(_credentialSettingsManager.Object);

            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();


            Assert.Equal(_defaultSampleIdentifier.Id,
                _awsConnectionManager.ActiveCredentialIdentifier.Id);
            Assert.Equal(_sampleRegion.Id, _awsConnectionManager.ActiveRegion.Id);
        }

        [Fact]
        public void UserActionConnectionState_WhenLoaded()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _credentialManager.Setup(x => x.IsLoginRequired(_defaultSampleIdentifier)).Returns(true);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            Assert.False(_awsConnectionManager.IsValidConnectionSettings());
            Assert.IsType<ConnectionState.UserAction>(_awsConnectionManager.ConnectionState);
            Assert.Equal(_defaultSampleIdentifier.Id,
                _awsConnectionManager.ActiveCredentialIdentifier.Id);
        }

        [Fact]
        public void ActiveCredentialsChosen_WhenCredentialSpecified()
        {
            _availableCredentials.Add(_sampleIdentifier.Id);
            _availableCredentials.Add(_sampleIdentifier2.Id);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier);
            WaitUntilConnectionStateIsStable();

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.GetRecentCredentialIdentifiers()[0]);

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier2);
            WaitUntilConnectionStateIsStable();
            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier2, _awsConnectionManager.GetRecentCredentialIdentifiers()[0]);
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.GetRecentCredentialIdentifiers()[1]);
        }

        [Fact]
        public void ActiveRegionChosen_WhenRegionSpecified()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            Assert.Equal(_defaultToolkitRegion.DisplayName, _awsConnectionManager.ActiveRegion.DisplayName);

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeRegion(_sampleRegion);
            WaitUntilConnectionStateIsStable();

            Assert.Equal(_sampleRegion, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleRegion.Id, _awsConnectionManager.GetRecentRegions()[0].Id);

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeRegion(_sampleRegion2);
            WaitUntilConnectionStateIsStable();

            Assert.Equal(_sampleRegion2, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleRegion2.Id, _awsConnectionManager.GetRecentRegions()[0].Id);
            Assert.Equal(_sampleRegion.Id, _awsConnectionManager.GetRecentRegions()[1].Id);
        }

        [Fact]
        public void ConnectionStateEventFired_WhenCredentialChanged()
        {
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            _telemetryLogger.Invocations.Clear();

            var receivedEvent = Assert.Raises<ConnectionStateChangeArgs>(
                a => _awsConnectionManager.ConnectionStateChanged += a,
                a => _awsConnectionManager.ConnectionStateChanged -= a,
                () => _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier));

            Assert.NotNull(receivedEvent);
            Assert.IsType<ConnectionState.ValidatingConnection>(_awsConnectionManager.ConnectionState);
            _telemetryLogger.Verify(mock => mock.Record(It.Is<Metrics>(m => HasSetCredentialsMetrics(m))), Times.Once);
        }

        [Fact]
        public void ConnectionStateEventFired_WhenRegionChanged()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            _telemetryLogger.Invocations.Clear();

            var receivedEvent = Assert.Raises<ConnectionStateChangeArgs>(
                a => _awsConnectionManager.ConnectionStateChanged += a,
                a => _awsConnectionManager.ConnectionStateChanged -= a,
                () => _awsConnectionManager.ChangeRegion(_sampleRegion));

            Assert.NotNull(receivedEvent);
            _telemetryLogger.Verify(mock => mock.Record(It.Is<Metrics>(m => HasSetRegionMetrics(m))), Times.Once);
        }

        [Fact]
        public void InvalidConnection_WhenSelectedCredentialRemoved()
        {
            _availableCredentials.Add(_sampleIdentifier.Id);
            _factoryMap.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, _sharedFactory.Object);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier);
            WaitUntilConnectionStateIsStable();

            Assert.IsType<ConnectionState.ValidConnection>(_awsConnectionManager.ConnectionState);
            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier.Id, _awsConnectionManager.ActiveCredentialIdentifier.Id);

            _sharedFactory.Raise(x => x.CredentialsChanged += null,
                new CredentialChangeEventArgs
                {
                    Removed = new List<ICredentialIdentifier> {_sampleIdentifier},
                    Modified = new List<ICredentialIdentifier>()
                });

            Assert.False(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Null(_awsConnectionManager.ActiveCredentialIdentifier);
        }


        [Fact]
        public void ConnectionRefreshed_WhenSelectedCredentialModified()
        {
            _availableCredentials.Add(_sampleIdentifier.Id);
            _factoryMap.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, _sharedFactory.Object);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier);
            WaitUntilConnectionStateIsStable();

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_sampleIdentifier.Id, _awsConnectionManager.ActiveCredentialIdentifier.Id);

            _connectionStateIsTerminalEvent.Reset();
            _sharedFactory.Raise(x => x.CredentialsChanged += null,
                new CredentialChangeEventArgs
                {
                    Modified = new List<ICredentialIdentifier> {_sampleIdentifier},
                    Removed = new List<ICredentialIdentifier>()
                });

            WaitUntilConnectionStateIsStable();

            AssertConnectionValidationStates();
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
        }


        [Fact]
        public void InvalidConnection_WhenSelectedCredentialFailsValidation()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _stsClient.Setup(x =>
                    x.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new ArgumentException());

            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            Assert.False(_awsConnectionManager.IsValidConnectionSettings());
            Assert.Equal(_defaultSampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
            Assert.IsType<ConnectionState.InvalidConnection>(_awsConnectionManager.ConnectionState);
        }

        [Fact]
        public void UpdateCancelled_WhenSettingsChangedConsecutively()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            Assert.True(_awsConnectionManager.IsValidConnectionSettings());

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeCredentialProvider(_sampleIdentifier);
            _awsConnectionManager.ChangeRegion(_sampleRegion);

            WaitUntilConnectionStateIsStable();

            AssertConnectionValidationStates();
            Assert.Equal(_sampleRegion, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleRegion.Id, _awsConnectionManager.GetRecentRegions()[0].Id);
            Assert.Equal(_sampleIdentifier, _awsConnectionManager.ActiveCredentialIdentifier);
        }

        [Fact]
        public void AccountIdEmpty_WhenLocalRegionSelected()
        {
            _availableCredentials.Add(_defaultSampleIdentifier.Id);
            _awsConnectionManager = new AwsConnectionManager(_fnStsClient,
                _credentialManager.Object, _telemetryLogger.Object, _regionProvider.Object);
            _awsConnectionManager.ConnectionStateChanged += CheckTerminalState;
            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
            WaitUntilConnectionStateIsStable();

            Assert.Equal(_defaultToolkitRegion.DisplayName, _awsConnectionManager.ActiveRegion.DisplayName);
            Assert.Equal("11222333444", _awsConnectionManager.ActiveAccountId);

            _connectionStateIsTerminalEvent.Reset();
            _awsConnectionManager.ChangeRegion(_sampleLocalRegion);
            WaitUntilConnectionStateIsStable();

            Assert.Equal(_sampleLocalRegion, _awsConnectionManager.ActiveRegion);
            Assert.Equal(_sampleLocalRegion.Id, _awsConnectionManager.GetRecentRegions()[0].Id);
            Assert.True(string.IsNullOrEmpty(_awsConnectionManager.ActiveAccountId));
        }

        private void WaitUntilConnectionStateIsStable()
        {
            _connectionStateIsTerminalEvent.WaitOne(10000);
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

        private AmazonSecurityTokenServiceClient SetupStsClient(AWSCredentials credentials, RegionEndpoint endpoint)
        {
            return _stsClient.Object;
        }

        private void CheckTerminalState(object sender, ConnectionStateChangeArgs e)
        {
            if (_awsConnectionManager.ConnectionState.IsTerminal)
            {
                _connectionStateIsTerminalEvent.Set();
            }

            _stateList.Add(_awsConnectionManager.ConnectionState);
        }

        private void AssertConnectionValidationStates()
        {
            var validatingIndex = _stateList.FindLastIndex(x => x is ConnectionState.ValidatingConnection);
            var validIndex = _stateList.FindLastIndex(x => x is ConnectionState.ValidConnection);
            Assert.True(validatingIndex < validIndex);
        }

        private bool HasSetCredentialsMetrics(Metrics metrics)
        {
            return metrics.Data.Any(metricDatum => metricDatum.MetricName == "aws_setCredentials");
        }

        private bool HasSetRegionMetrics(Metrics metrics)
        {
            return metrics.Data.Any(metricDatum => metricDatum.MetricName == "aws_setRegion");
        }
    }
}
