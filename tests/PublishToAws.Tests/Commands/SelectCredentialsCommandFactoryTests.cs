using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Publishing.Common;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class SelectCredentialsCommandFactoryTests
    {
        private const string SampleStackName = "sampleStack";
        private static readonly ICredentialIdentifier SampleCredentialA = new SharedCredentialIdentifier("A");
        private static readonly ICredentialIdentifier SampleCredentialB = new SharedCredentialIdentifier("B");
        private static readonly ToolkitRegion SampleRegionA = new ToolkitRegion() { Id = "A" };
        private static readonly ToolkitRegion SampleRegionB = new ToolkitRegion() { Id = "B" };

        private readonly PublishContextFixture _contextFixture = new PublishContextFixture();
        private readonly PublishToAwsDocumentViewModel _viewModel;
        private readonly Mock<IDialogFactory> _dialogFactory = new Mock<IDialogFactory>();
        private readonly Mock<ICredentialSelectionDialog> _credentialsDialog = new Mock<ICredentialSelectionDialog>();
        private readonly ICommand _selectDialogCommand;
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitShellProvider;

        public SelectCredentialsCommandFactoryTests()
        {
            _viewModel =
                new PublishToAwsDocumentViewModel(new PublishApplicationContext(_contextFixture.PublishContext))
                {
                    PublishedArtifactId = SampleStackName,
                    PublishDestination = new PublishRecommendation(new RecommendationSummary()
                    {
                        DeploymentType = DeploymentTypes.CloudFormationStack,
                    }),
                };

            _viewModel.Connection.Region = SampleRegionA;
            _viewModel.Connection.CredentialsId = SampleCredentialA;

            _selectDialogCommand = SelectCredentialsCommandFactory.Create(_viewModel);
            SetupMocks();
        }

        private void SetupMocks()
        {
            ToolkitHost.Setup(mock => mock.GetDialogFactory()).Returns(_dialogFactory.Object);

            _dialogFactory.Setup(mock => mock.CreateCredentialsSelectionDialog())
                .Returns(_credentialsDialog.Object);

            _credentialsDialog.SetupGet(mock => mock.CredentialIdentifier).Returns(SampleCredentialB);
            _credentialsDialog.SetupGet(mock => mock.Region).Returns(SampleRegionB);
            SetupShowDialogToReturn(true);
        }

        [Fact]
        public void CanExecute()
        {
            _viewModel.ViewStage = PublishViewStage.Target;

            Assert.True(_selectDialogCommand.CanExecute(null));
        }

        [Fact]
        public void CanExecute_IsPublishing()
        {
            _viewModel.IsPublishing = true;

            Assert.False(_selectDialogCommand.CanExecute(null));
        }

        [Theory]
        [InlineData(PublishViewStage.Configure)]
        [InlineData(PublishViewStage.Publish)]
        public void CanExecute_NonTargetStage(PublishViewStage stage)
        {
            _viewModel.ViewStage = stage;

            Assert.False(_selectDialogCommand.CanExecute(null));
        }

        [Fact]
        public void Execute()
        {
            _viewModel.ViewStage = PublishViewStage.Target;

            _selectDialogCommand.Execute(null);

            _credentialsDialog.Verify(mock => mock.Show(), Times.Once);
            Assert.Equal(SampleCredentialB, _viewModel.PublishContext.ConnectionManager.ActiveCredentialIdentifier);
            Assert.Equal(SampleRegionB, _viewModel.PublishContext.ConnectionManager.ActiveRegion);
        }

        [Fact]
        public void Execute_DialogCancelled()
        {
            _viewModel.ViewStage = PublishViewStage.Target;
            SetupShowDialogToReturn(false);

            _selectDialogCommand.Execute(null);

            _credentialsDialog.Verify(mock => mock.Show(), Times.Once);
            Assert.NotEqual(SampleCredentialB, _viewModel.PublishContext.ConnectionManager.ActiveCredentialIdentifier);
            Assert.NotEqual(SampleRegionB, _viewModel.PublishContext.ConnectionManager.ActiveRegion);
        }

        private void SetupShowDialogToReturn(bool result)
        {
            _credentialsDialog.Setup(mock => mock.Show()).Returns(result);
        }
    }
}
