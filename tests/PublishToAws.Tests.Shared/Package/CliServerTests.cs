using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Services;
using Amazon.Runtime;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Package
{
    public class CliServerTests : IDisposable
    {
        private delegate void TryGetRestApiClientDelegate(
            Func<Task<AWSCredentials>> credentialsGenerator,
            out IRestAPIClient restApiClient);

        private delegate void TryGetDeploymentCommunicationClientDelegate(
            out IDeploymentCommunicationClient deploymentClient);

        private readonly Mock<IServerModeSession> _server = new Mock<IServerModeSession>();
        private readonly Mock<IRestAPIClient> _restClient = new Mock<IRestAPIClient>();
        private readonly Mock<IDeploymentCommunicationClient> _deployClient = new Mock<IDeploymentCommunicationClient>();
        private readonly CliServer _sut;

        public CliServerTests()
        {
            _server.Setup(mock => mock.IsAlive(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            IRestAPIClient restClient;
            IDeploymentCommunicationClient deployClient;
            _server.Setup(mock => mock.TryGetRestAPIClient(It.IsAny<Func<Task<AWSCredentials>>>(), out restClient))
                .Callback(new TryGetRestApiClientDelegate((Func<Task<AWSCredentials>> credentialsGenerator,
                    out IRestAPIClient restApiClient) =>
                {
                    restApiClient = _restClient.Object;
                }))
                .Returns(true);
            _server.Setup(mock => mock.TryGetDeploymentCommunicationClient(out deployClient))
                .Callback(new TryGetDeploymentCommunicationClientDelegate((out IDeploymentCommunicationClient deploymentClient) =>
                {
                    deploymentClient = _deployClient.Object;
                }))
                .Returns(true);

            _sut = new CliServer(_server.Object);
        }

        [Fact]
        public async Task StartAsync()
        {
            await _sut.StartAsync(CancellationToken.None);
            _server.Verify(mock => mock.Start(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartAsync_ServerException()
        {
            _server.Setup(mock => mock.Start(It.IsAny<CancellationToken>())).Throws<Exception>();

            await Assert.ThrowsAsync<Exception>(async () => await _sut.StartAsync(CancellationToken.None));
        }

        [Fact]
        public async Task StartAsync_MultipleCalls()
        {
            await _sut.StartAsync(CancellationToken.None);
            await _sut.StartAsync(CancellationToken.None);

            _server.Verify(mock => mock.Start(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetRestClient()
        {
            await _sut.StartAsync(CancellationToken.None);
            var client = _sut.GetRestClient(GetCredentials);

            Assert.NotNull(client);
            Assert.Equal(_restClient.Object, client);
        }

        [Fact]
        public async Task GetRestClient_NoClientAvailable()
        {
            StubTryGetRestAPIClientReturnsFalse();
            await _sut.StartAsync(CancellationToken.None);
            Assert.Throws<Exception>(() => _sut.GetRestClient(GetCredentials));
        }

        private void StubTryGetRestAPIClientReturnsFalse()
        {
            IRestAPIClient restClient;
            _server.Setup(mock => mock.TryGetRestAPIClient(It.IsAny<Func<Task<AWSCredentials>>>(), out restClient))
                .Returns(false);
        }

        [Fact]
        public async Task GetRestClient_CliServerThrows()
        {
            StubTryGetRestAPIClientThrows();
            await _sut.StartAsync(CancellationToken.None);
            Assert.Throws<Exception>(() => _sut.GetRestClient(GetCredentials));
        }

        private void StubTryGetRestAPIClientThrows()
        {
            IRestAPIClient restClient;
            _server.Setup(mock => mock.TryGetRestAPIClient(It.IsAny<Func<Task<AWSCredentials>>>(), out restClient))
                .Throws<Exception>();
        }

        [Fact]
        public async Task GetDeploymentClient()
        {
            await _sut.StartAsync(CancellationToken.None);
            var client = _sut.GetDeploymentClient();

            Assert.NotNull(client);
            Assert.Equal(_deployClient.Object, client);
        }

        [Fact]
        public async Task GetDeploymentClient_NoClientAvailable()
        {
            StubTryGetDeploymentCommunicationClientReturnsFalse();
            await _sut.StartAsync(CancellationToken.None);
            Assert.Throws<Exception>(() => _sut.GetDeploymentClient());
        }

        private void StubTryGetDeploymentCommunicationClientReturnsFalse()
        {
            IDeploymentCommunicationClient deployClient;
            _server.Setup(mock => mock.TryGetDeploymentCommunicationClient(out deployClient))
                .Returns(false);
        }

        [Fact]
        public async Task GetDeploymentClient_CliServerThrows()
        {
            StubTryGetDeploymentCommunicationClientThrows();
            await _sut.StartAsync(CancellationToken.None);
            Assert.Throws<Exception>(() => _sut.GetDeploymentClient());
        }

        private void StubTryGetDeploymentCommunicationClientThrows()
        {
            IDeploymentCommunicationClient deployClient;
            _server.Setup(mock => mock.TryGetDeploymentCommunicationClient(out deployClient))
                .Throws<Exception>();
        }

        [Fact]
        public async Task Disconnect()
        {
            var disconnectEvent = new ManualResetEvent(false);
            _sut.Disconnect += (sender, args) =>
            {
                disconnectEvent.Set();
            };

            await _sut.StartAsync(CancellationToken.None);
            Assert.False(disconnectEvent.WaitOne(0));

            // Kill the server and wait for a Disconnect event
            _server.Setup(mock => mock.IsAlive(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Assert.True(disconnectEvent.WaitOne(CliServer.HealthCheckIntervalMs * 2));
        }

        public void Dispose()
        {
            _sut?.Dispose();
        }

        private Task<AWSCredentials> GetCredentials()
        {
            return Task.FromResult<AWSCredentials>(null);
        }
    }
}
