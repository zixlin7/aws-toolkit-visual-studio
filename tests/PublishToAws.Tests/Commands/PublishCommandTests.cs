using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;
using Amazon.AWSToolkit.Tests.Publishing.Views;

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
        private readonly FakeConfirmPublishDialog _confirmationDialog = new FakeConfirmPublishDialog();
        private readonly PublishCommand _sut;

        public PublishCommandTests()
        {
            _sut = new PublishCommand(ViewModel, _commandFixture.ShellProvider.Object, _ => _confirmationDialog);
            _commandFixture.JoinableTaskFactory.Run(async () => await _commandFixture.SetupNewPublishAsync());

            SetupInitialPublish();
            SetupApplyConfigSettingsAsync(new ValidationResult());
        }

        [StaFact]
        public async Task ExecuteCommand()
        {
            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Publish, ViewModel.ViewStage);
            await _commandFixture.AssertConfirmationIsNotSilencedAsync();
        }

        public class FailureBannerEnabledSpy
        {
            private readonly PublishProjectViewModel _viewModel;
            public int FalseCount;
            public int TrueCount;

            public FailureBannerEnabledSpy(PublishProjectViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.PropertyChanged += OnHandle;
            }

            public void OnHandle(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(PublishProjectViewModel.IsFailureBannerEnabled))
                {
                    if (_viewModel.IsFailureBannerEnabled)
                    {
                        TrueCount += 1;
                    }
                    else
                    {
                        FalseCount += 1;
                    }
                }
            }
        }

        [StaFact]
        public async Task HasFailureBannerDisabledAtStartOfPublish()
        {
            ViewModel.PublishProjectViewModel.IsFailureBannerEnabled = true;
            FailureBannerEnabledSpy spy = new FailureBannerEnabledSpy(ViewModel.PublishProjectViewModel);

            await _sut.ExecuteAsync(null);

            Assert.Equal(1, spy.FalseCount);
            Assert.Equal(0, spy.TrueCount);
        }

        [StaFact]
        public async Task ExecuteCommand_WithSuppressedConfirmations()
        {
            var publishSettings = await ViewModel.PublishContext.PublishSettingsRepository.GetAsync();
            publishSettings.SilencedPublishConfirmations.Add(ViewModel.ProjectGuid.ToString());

            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Publish, ViewModel.ViewStage);
        }

        [StaFact]
        public async Task ExecuteCommand_SuppressFutureConfirmations()
        {
            _confirmationDialog.SilenceFutureConfirmations = true;
            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Publish, ViewModel.ViewStage);

            await _commandFixture.AssertConfirmationIsSilencedAsync();
        }

        [StaFact]
        public async Task ExecuteCommand_CancelConfirmation()
        {
            _confirmationDialog.ShowModalResult = false;
            await _sut.ExecuteAsync(null);

            Assert.NotEqual(PublishViewStage.Publish, ViewModel.ViewStage);
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

            await _sut.ExecuteAsync(null);

            ShellProvider.Verify(x => x.OutputToHostConsole(It.Is<string>(s => s.Contains("error getting status")), true), Times.Once);

            Assert.Equal(PublishViewStage.Publish, ViewModel.ViewStage);
        }

        [Fact]
        public void CanExecute()
        {
            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_IsLoading()
        {
            ViewModel.IsLoading = true;
            Assert.False(_sut.CanExecute(null));
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
