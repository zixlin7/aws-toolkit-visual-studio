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
        public void CreateMessageGroup()
        {
            _sut.CreateMessageGroup("hello", "world");

            var deploymentMessage = Assert.Single(_sut.DeploymentMessages);
            Assert.Equal("hello", deploymentMessage.Name);
            Assert.Equal("world", deploymentMessage.Description);
            Assert.Empty(deploymentMessage.Message);
        }

        [Fact]
        public void CreateMessageGroup_CollapsesPreviousGroup()
        {
            _sut.CreateMessageGroup("hello", "world");
            _sut.CreateMessageGroup("foo", "bar");

            var collapsedMessage = Assert.Single(_sut.DeploymentMessages, g => g.Name == "hello");
            Assert.False(collapsedMessage.IsExpanded);

            var expandedMessage = Assert.Single(_sut.DeploymentMessages, g => g.Name == "foo");
            Assert.True(expandedMessage.IsExpanded);
        }

        [Fact]
        public void AppendLineDeploymentMessage()
        {
            _sut.CreateMessageGroup("hello", "world");
            _sut.AppendLineDeploymentMessage("hello");
            _sut.AppendLineDeploymentMessage("world");

            var deploymentMessage = Assert.Single(_sut.DeploymentMessages);
            Assert.Equal(string.Format("hello{0}world{0}", Environment.NewLine), deploymentMessage.Message);
        }

        [Fact]
        public void AppendLineDeploymentMessage_WithoutMessageGroup()
        {
            Assert.Throws<Exception>(() => _sut.AppendLineDeploymentMessage("hello"));
        }

        [Fact]
        public void AppendLineDeploymentMessage_AppendsToLatestGroup()
        {
            _sut.CreateMessageGroup("hello", "world");
            _sut.CreateMessageGroup("foo", "bar");
            _sut.CreateMessageGroup("baz", "baz");

            _sut.AppendLineDeploymentMessage("hello world");

            var emptyGroup1 = Assert.Single(_sut.DeploymentMessages, g => g.Name == "hello");
            Assert.Empty(emptyGroup1.Message);
            var emptyGroup2 = Assert.Single(_sut.DeploymentMessages, g => g.Name == "foo");
            Assert.Empty(emptyGroup2.Message);
            var nonEmptyGroup = Assert.Single(_sut.DeploymentMessages, g => g.Name == "baz");
            Assert.Contains("hello world", nonEmptyGroup.Message);
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
