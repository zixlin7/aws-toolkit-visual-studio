using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class PublishProjectViewModelTests
    {
        private readonly PublishToAwsFixture _publishToAwsFixture = new PublishToAwsFixture();

        private CancellationToken CancellationToken => _publishToAwsFixture.PublishContext.PublishPackage.DisposalToken;
        private DeployToolControllerFixture DeployToolControllerFixture =>
            _publishToAwsFixture.DeployToolControllerFixture;

        private string SessionId => PublishToAwsFixture.SampleSessionId;

        private readonly PublishProjectViewModel _sut;

        public PublishProjectViewModelTests()
        {
            _sut = new PublishProjectViewModel(_publishToAwsFixture.PublishApplicationContext);
            _sut.SessionId = SessionId;
            _sut.SetDeployToolController(DeployToolControllerFixture.DeployToolController.Object);
        }

        [Fact]
        public void AppendLinePublishProgress()
        {
            _sut.AppendLinePublishProgress("hello");
            _sut.AppendLinePublishProgress("world");

            Assert.Equal(string.Format("hello{0}world{0}", Environment.NewLine), _sut.PublishProgress);
        }

        [Fact]
        public void IsPublishedScope()
        {
            _sut.IsPublishing = false;

            using (_sut.IsPublishedScope())
            {
                Assert.True(_sut.IsPublishing);
            }

            Assert.False(_sut.IsPublishing);
        }

        [StaFact]
        public async Task UpdatePublishedResourcesAsync()
        {
            await _sut.UpdatePublishedResourcesAsync(CancellationToken);

            DeployToolControllerFixture.DeployToolController.Verify(mock => mock.GetDeploymentDetailsAsync(SessionId, CancellationToken), Times.Once);

            Assert.NotEmpty(_sut.PublishResources);
            Assert.Equal(DeployToolControllerFixture.GetDeploymentDetailsAsyncResponse.CloudApplicationName, _sut.PublishedArtifactId);
        }

        [StaFact]
        public async Task RefreshPublishResources_EcrRepoWorkaround()
        {
            DeployToolControllerFixture.GetDeploymentDetailsAsyncResponse.CloudApplicationName = null;
            _sut.ArtifactType = DeploymentArtifact.ElasticContainerRegistry;

            await _sut.UpdatePublishedResourcesAsync(CancellationToken);

            DeployToolControllerFixture.DeployToolController.Verify(mock => mock.GetDeploymentDetailsAsync(SessionId, CancellationToken), Times.Once);

            Assert.NotEmpty(_sut.PublishResources);
            Assert.Equal("some-ecr-repo", _sut.PublishedArtifactId);
        }

        [StaFact]
        public async Task RefreshPublishResources_NoSession()
        {
            _sut.SessionId = string.Empty;
            var exception = await Assert.ThrowsAsync<Exception>(async () => await _sut.UpdatePublishedResourcesAsync(CancellationToken));
            Assert.Contains("No deployment session available", exception.Message);
        }

        [StaFact]
        public async Task RefreshPublishResources_ThrowsException()
        {
            DeployToolControllerFixture.StubGetDeploymentDetailsAsyncThrows();

            var exception = await Assert.ThrowsAsync<Exception>(async () => await _sut.UpdatePublishedResourcesAsync(CancellationToken));

            Assert.Contains("simulated service error", exception.Message);
            Assert.Empty(_sut.PublishResources);
            Assert.True(string.IsNullOrWhiteSpace(_sut.PublishedArtifactId));
        }

        [StaFact]
        public async Task PublishProjectAsync()
        {
            DeployToolControllerFixture.SetupGetDeploymentStatusAsync(
                SamplePublishData.GetDeploymentStatusOutputs.Executing,
                SamplePublishData.GetDeploymentStatusOutputs.Success,
                SamplePublishData.GetDeploymentStatusOutputs.Success);

            var result = await _sut.PublishProjectAsync();

            Assert.True(result.IsSuccess);
            DeployToolControllerFixture.AssertStartDeploymentCalledTimes(1);
            DeployToolControllerFixture.AssertGetDeploymentCalledTimes(3);
        }

        [StaFact]
        public async Task PublishProjectAsync_FailureStatus()
        {
            var failureStatus = SamplePublishData.GetDeploymentStatusOutputs.Fail;

            DeployToolControllerFixture.SetupGetDeploymentStatusAsync(failureStatus);

            var result = await _sut.PublishProjectAsync();

            Assert.False(result.IsSuccess);
            Assert.Contains(failureStatus.Exception.ErrorCode, result.ErrorCode);
            Assert.Contains(failureStatus.Exception.Message, result.ErrorMessage);
            DeployToolControllerFixture.AssertStartDeploymentCalledTimes(1);
            DeployToolControllerFixture.AssertGetDeploymentCalledTimes(2);
        }

        [StaFact]
        public async Task PublishProjectAsync_WhenExceptionThrown()
        {
            DeployToolControllerFixture.StubStartDeploymentAsyncThrows("service failure");

            var result = await _sut.PublishProjectAsync();

            Assert.False(result.IsSuccess);
            Assert.Equal("service failure", result.ErrorMessage);

            DeployToolControllerFixture.AssertStartDeploymentCalledTimes(1);
            DeployToolControllerFixture.AssertGetDeploymentCalledTimes(0);
        }
    }
}
