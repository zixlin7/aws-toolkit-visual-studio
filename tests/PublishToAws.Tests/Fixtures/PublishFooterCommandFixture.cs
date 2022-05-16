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

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

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

        public void SetPreviousPublishDestination(PublishDestinationBase publishDestination)
        {
            base.SetCachedPublishDestination(publishDestination);
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
        public JoinableTaskFactory JoinableTaskFactory { get; }

        public PublishFooterCommandFixture()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskCollection = new JoinableTaskContext();
#pragma warning restore VSSDK005
            JoinableTaskFactory = taskCollection.Factory;

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
            ViewModel.ProjectGuid = Guid.NewGuid();
            ViewModel.ProjectPath = $"c:\\deploy-project\\{ViewModel.ProjectGuid}\\my.csproj";
        }

        public void SetupNewPublish()
        {
            ViewModel.IsRepublish = false;
            ViewModel.PublishDestination = ViewModel.Recommendations?.FirstOrDefault();
        }

        public void SetupRepublish()
        {
            ViewModel.IsRepublish = true;
            ViewModel.PublishDestination = ViewModel.RepublishTargets?.FirstOrDefault();
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

        public async Task AssertConfirmationIsSilencedAsync()
        {
            var publishSettings = await ViewModel.PublishContext.PublishSettingsRepository.GetAsync();
            Assert.Contains(ViewModel.ProjectGuid.ToString(), publishSettings.SilencedPublishConfirmations);
        }

        public async Task AssertConfirmationIsNotSilencedAsync()
        {
            var publishSettings = await ViewModel.PublishContext.PublishSettingsRepository.GetAsync();
            Assert.DoesNotContain(ViewModel.ProjectGuid.ToString(), publishSettings.SilencedPublishConfirmations);
        }

        public void AddMissingRequirement(string requirementName)
        {
            ViewModel.SystemCapabilities.Add(new TargetSystemCapability(new SystemCapabilitySummary()
            {
                Name = requirementName,
            }));
        }
    }
}
