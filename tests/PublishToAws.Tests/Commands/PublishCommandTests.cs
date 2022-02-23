using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class PublishCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private Mock<IAWSToolkitShellProvider> ShellProvider => _commandFixture.ShellProvider;
        private readonly PublishCommand _sut;

        public PublishCommandTests()
        {
            _sut = new PublishCommand(ViewModel, _commandFixture.ShellProvider.Object);
            _commandFixture.SetupNewPublish();
        }

        [StaFact]
        public async Task ExecuteCommand()
        {
            SetupInitialPublish();
            SetupApplyConfigSettingsAsync(new ValidationResult());

            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Publish, ViewModel.ViewStage);
        }


        [StaFact]
        public async Task ExecuteCommand_ValidationFails()
        {
            var validationResult = new ValidationResult();
            validationResult.AddError("id", "error");
            SetupApplyConfigSettingsAsync(validationResult);

            await _sut.ExecuteAsync(null);

            ShellProvider.Verify(x => x.OutputToHostConsole(It.Is<string>(s => s.Contains("One or more configuration settings")), true), Times.Once);
            
            Assert.NotEqual(PublishViewStage.Publish, ViewModel.ViewStage);
        }

        [StaFact]
        public async Task ExecuteCommand_PublishFails()
        {
            SetupStartDeploymentToThrow();
            SetupApplyConfigSettingsAsync(new ValidationResult());

            await _sut.ExecuteAsync(null);

            ShellProvider.Verify(x => x.OutputToHostConsole(It.Is<string>(s => s.Contains("error getting status")), true), Times.Once);

            Assert.Equal(PublishViewStage.Publish, ViewModel.ViewStage);
        }


        [Fact]
        public void CanExecute_HasValidationErrors()
        {
            ViewModel.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>()
            {
                new ConfigurationDetail() {ValidationMessage = "some-error",}
            };
            Assert.False(_sut.CanExecute(null));
        }

        private void SetupApplyConfigSettingsAsync(ValidationResult validationResult)
        {
            _commandFixture.DeployToolController.Setup(mock => mock.ApplyConfigSettingsAsync(It.IsAny<string>(),
                It.IsAny<IList<ConfigurationDetail>>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);
        }

        private void SetupStartDeploymentToThrow()
        {
            _commandFixture.DeployToolController.Setup(mock => mock.StartDeploymentAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("error getting status"));
        }

        private void SetupInitialPublish()
        {
            _commandFixture.DeployToolController.Setup(mock => mock.StartDeploymentAsync(It.IsAny<string>()));
            _commandFixture.DeployToolController.Setup(mock => mock.GetDeploymentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(new GetDeploymentStatusOutput() { Status = DeploymentStatus.Success });
        }
    }
}
