using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Publishing.Common;

using AWS.Deploy.ServerMode.Client;

using Moq;

namespace Amazon.AWSToolkit.Tests.Publishing.Fixtures
{
    /// <summary>
    /// Extends <see cref="PublishToAwsDocumentViewModel"/> to provide
    /// writable access to SessionId for testing purposes.
    /// </summary>
    public class TestPublishToAwsDocumentViewModel : PublishToAwsDocumentViewModel
    {
        public TestPublishToAwsDocumentViewModel(PublishApplicationContext publishApplicationContext)
            : base(publishApplicationContext)
        {
        }

        public new string SessionId
        {
            get => base.SessionId;
            set => base.SessionId = value;
        }

        public async Task ExposedLoadOptionsButtonSettingsAsync()
        {
            await base.LoadOptionsButtonSettingsAsync();
        }
    }

    /// <summary>
    /// Extends <see cref="TargetViewCommand"/> for testing purposes
    /// </summary>
    public class TestTargetViewCommand : TargetViewCommand
    {
        public TestTargetViewCommand(PublishToAwsDocumentViewModel viewModel) : base(viewModel)
        {
        }

        protected override Task ExecuteCommandAsync()
        {
            throw new NotImplementedException();
        }

        protected override bool CanExecuteCommand()
        {
            return true;
        }
    }

    /// <summary>
    /// Setup that is common to tests that relate to the Publish Panel's Footer Commands
    /// </summary>
    public class PublishFooterCommandFixture
    {
        private readonly PublishContextFixture _publishContextFixture = new PublishContextFixture();
        public Mock<IDeployToolController> DeployToolController = new Mock<IDeployToolController>();
        public Mock<IDeploymentCommunicationClient> DeploymentCommunicationClient = new Mock<IDeploymentCommunicationClient>();
        public TestPublishToAwsDocumentViewModel ViewModel { get; }
        public Mock<IAWSToolkitShellProvider> ShellProvider => _publishContextFixture.ToolkitShellProvider;

        public PublishFooterCommandFixture()
        {
            ViewModel =
                new TestPublishToAwsDocumentViewModel(
                    new PublishApplicationContext(_publishContextFixture.PublishContext))
                {
                    StackName = "some-application-name",
                    SessionId = "session-id",
                    Recommendations = new ObservableCollection<PublishRecommendation>(
                        new List<PublishRecommendation>()
                        {
                            new PublishRecommendation(new RecommendationSummary() {RecipeId = "recipe-1", TargetService = "sample-service-1"}),
                            new PublishRecommendation(new RecommendationSummary() {RecipeId = "recipe-2", TargetService = "sample-service-2"})
                        }),
                    RepublishTargets = new ObservableCollection<RepublishTarget>(
                        new List<RepublishTarget>()
                        {
                            new RepublishTarget(new ExistingDeploymentSummary() {Name = "republishStack1", RecipeId = "republish-recipe-1"}),
                            new RepublishTarget(new ExistingDeploymentSummary() {Name = "republishStack1", RecipeId = "republish-recipe-2"})
                        }),
                    DeployToolController =  DeployToolController.Object
                };

            ViewModel.DeploymentClient = DeploymentCommunicationClient.Object;
            ViewModel.Recommendation = ViewModel.Recommendations.First();
            ViewModel.RepublishTarget = ViewModel.RepublishTargets.First();
        }

        public void StubStartSessionToReturn(SessionDetails sessionDetails)
        {
            DeployToolController.Setup(mock =>
                    mock.StartSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionDetails);
        }

        public void StubGetRepublishTargetsAsync(ICollection<RepublishTarget> republishTargets)
        {
            DeployToolController.Setup(mock =>
                    mock.GetRepublishTargetsAsync(It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(republishTargets);
        }

        public void StubGetRecommendationsAsync(ICollection<PublishRecommendation> recommendations)
        {
            DeployToolController.Setup(mock =>
                    mock.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendations);
        }

        public void StubStartSessionThrows()
        {
            DeployToolController.Setup(mock =>
                    mock.StartSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SessionException("unable to establish session", null));
        }
    }
}
