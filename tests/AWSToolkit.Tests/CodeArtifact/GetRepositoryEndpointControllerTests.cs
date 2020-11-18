using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CodeArtifact.Controller;
using Amazon.AWSToolkit.Shared;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using Moq;
using System;
using System.Linq;
using System.Windows;
using Xunit;

namespace AWSToolkit.Tests.CodeArtifact
{
    public class GetRepositoryEndpointControllerTests
    {
        private readonly TelemetryFixture _telemetryFixture = new TelemetryFixture();
        private readonly Mock<IAmazonCodeArtifact> _mockCodeArtifactClient = new Mock<IAmazonCodeArtifact>();
        private readonly Mock<IAWSToolkitShellProvider> _mockShell = new Mock<IAWSToolkitShellProvider>();
        private readonly GetRepositoryEndpointController _sut;
        const string endpoint = "https://test-domain-123456789012.d.codeartifact.us-west-2.amazonaws.com/maven/test/";

        public GetRepositoryEndpointControllerTests()
        {
            _sut = new GetRepositoryEndpointController(_mockShell.Object, _telemetryFixture.TelemetryLogger.Object);
        }


        [StaFact]
        public void UrlCopiedToClipboard()
        {
            SetupValidGetRepositoryEndpointCall();
            var results = _sut.Execute(_mockCodeArtifactClient.Object, "test-domain", "test");
            string expectedEndpoint = string.Format("{0}v3/index.json", endpoint.Replace("maven", "nuget"));
            Assert.Equal(Clipboard.GetText(), expectedEndpoint);
            _mockShell.Verify(x => x.UpdateStatus(It.IsAny<string>()), Times.Once);
            _mockShell.Verify(x => x.ShowError(It.IsAny<string>()), Times.Never);
            Assert.True(results.Success);
            AssertMetric(Result.Succeeded);
        }

        [StaFact]
        public void GenerateUrl()
        {
            SetupValidGetRepositoryEndpointCall();
            string url = _sut.GenerateURL(_mockCodeArtifactClient.Object, "test-domain", "test");
            string expectedEndpoint = string.Format("{0}v3/index.json", endpoint.Replace("maven", "nuget"));
            Assert.Equal(url, expectedEndpoint);
        }

        [StaFact]
        public void GenerateUrlThrowsException()
        {
            _mockCodeArtifactClient.Setup(client => client.GetRepositoryEndpoint(It.IsAny<GetRepositoryEndpointRequest>())).Throws(new ValidationException("some error"));
            Assert.Null(_sut.GenerateURL(_mockCodeArtifactClient.Object, "test-domain", "test"));
            _mockShell.Verify(x => x.ShowError(It.IsAny<string>()), Times.Once);
        }

        private void SetupValidGetRepositoryEndpointCall()
        {
            var getRepoRequest = new GetRepositoryEndpointResponse()
            {
                RepositoryEndpoint = endpoint
            };
            _mockCodeArtifactClient.Setup(client => client.GetRepositoryEndpoint(It.IsAny<GetRepositoryEndpointRequest>())).Returns(getRepoRequest);
            _mockShell.Setup(shell => shell.UpdateStatus(It.IsAny<string>()));
        }

        [StaFact]
        public void GenerateUrlThrowsExceptionFullOperation()
        {
            _mockCodeArtifactClient.Setup(client => client.GetRepositoryEndpoint(It.IsAny<GetRepositoryEndpointRequest>())).Throws(new ValidationException("some error"));
            var results = _sut.Execute(_mockCodeArtifactClient.Object, "test-domain", "test");
            Assert.False(results.Success);
            AssertMetric(Result.Failed);
        }

        private void AssertMetric(Result result)
        {
            _telemetryFixture.AssertTelemetryRecordCalls(1);
            _telemetryFixture.AssertCodeArtifactMetrics(_telemetryFixture.LoggedMetrics.Single(),
                result, "codeartifact_getRepoUrl", "nuget");
        }

    }
}
