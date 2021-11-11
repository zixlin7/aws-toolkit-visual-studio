﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Tests.Publishing.Common;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class PublishToAwsDocumentViewModelTests
    {
        private readonly Mock<IDeployToolController> _deployToolController = new Mock<IDeployToolController>();
        private readonly Mock<IDeploymentCommunicationClient> _deployClient = new Mock<IDeploymentCommunicationClient>();
        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();
        private readonly PublishToAwsDocumentViewModel _sut;
        private readonly TestPublishToAwsDocumentViewModel _exposedTestViewModel;

        private static readonly string _sampleSessionId = Guid.NewGuid().ToString();
        private static readonly string _sampleApplicationName = $"Sample-app-{Guid.NewGuid()}";

        private static readonly GetDeploymentStatusOutput DeploymentResultExecuting =
            new GetDeploymentStatusOutput() { Status = DeploymentStatus.Executing };
            
        private static readonly GetDeploymentStatusOutput DeploymentResultSuccess =
            new GetDeploymentStatusOutput() { Status = DeploymentStatus.Success };

        private static readonly GetDeploymentStatusOutput DeploymentResultFail =
            new GetDeploymentStatusOutput()
            {
                Status = DeploymentStatus.Error,
                Exception = new DeployToolExceptionSummary()
                {
                    ErrorCode = "FailedToDeployCdkApplication", Message = "Failed to deploy cdk app"
                }
            };

        private readonly ApplyConfigSettingsOutput _sampleSetOptionSettingOutput =
            new ApplyConfigSettingsOutput();

        private readonly GetDeploymentDetailsOutput _deploymentDetails;

        private readonly List<TargetSystemCapability> _sampleGetCompatibilityOutput =
            new List<TargetSystemCapability>();

        private readonly List<PublishRecommendation> _sampleRecommendations;

        private readonly List<RepublishTarget> _sampleRepublishTargets;

        private readonly ObservableCollection<ConfigurationDetail> _sampleConfigurationDetails;

        private readonly PublishContextFixture _publishContextFixture = new PublishContextFixture();

        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion()
        {
            PartitionId = "SamplePartition", DisplayName = "SampleRegion", Id = "region1",
        };

        private readonly CancellationToken _cancelToken;

        public PublishToAwsDocumentViewModelTests()
        {
            StubStartSessionToReturn(CreateSampleSessionDetails());

            _sampleRecommendations = SamplePublishData.CreateSampleRecommendations();
            _sampleConfigurationDetails = CreateSampleConfigurationDetails(1);
            _deploymentDetails = CreateSampleDeploymentDetails();

            StubGetRecommendationsToReturn(_sampleRecommendations);

            _sampleRepublishTargets = SamplePublishData.CreateSampleRepublishTargets();
            StubGetRepublishTargetsToReturn(_sampleRepublishTargets);


            _sut = new PublishToAwsDocumentViewModel(
                new PublishApplicationContext(_publishContextFixture.PublishContext))
            {
                DeploymentClient = _deployClient.Object, Region = _sampleRegion, DeployToolController = _deployToolController.Object
            };

            _exposedTestViewModel = new TestPublishToAwsDocumentViewModel(new PublishApplicationContext(_publishContextFixture.PublishContext));
            _cancelToken = _publishContextFixture.PublishContext.PublishPackage.DisposalToken;
        }

        private SessionDetails CreateSampleSessionDetails()
        {
            return new SessionDetails()
            {
                SessionId = _sampleSessionId, DefaultApplicationName = _sampleApplicationName,
            };
        }

        private void StubStartSessionToReturn(SessionDetails sessionDetails)
        {
            _deployToolController.Setup(mock => mock.StartSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionDetails);
        }
        private void StubGetRecommendationsToReturn(ICollection<PublishRecommendation> recommendations)
        {
            _deployToolController.Setup(mock => mock.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(recommendations);
        }

        private void StubGetRepublishTargetsToReturn(ICollection<RepublishTarget> republishTargets)
        {
            _deployToolController.Setup(mock => mock.GetRepublishTargetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(republishTargets);
        }

        [Fact]
        public async Task ShouldInitializeAsRepublish()
        {
            //arrange
            await _sut.StartDeploymentSession(_cancelToken);
            StubGetRepublishTargetsToReturn(_sampleRepublishTargets);
            //act
            await _sut.InitializePublishTargets(_cancelToken);
            //assert
            Assert.True(_sut.RepublishTargets.Any());
            Assert.True(_sut.IsRepublish);
        }

        [Fact]
        public async Task ShouldInitializeAsPublish()
        {
            //arrange
            await _sut.StartDeploymentSession(_cancelToken);
            StubGetRepublishTargetsToReturn(Array.Empty<RepublishTarget>());
            //act
            await _sut.InitializePublishTargets(_cancelToken);
            //assert
            Assert.False(_sut.RepublishTargets.Any());
            Assert.False(_sut.IsRepublish);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesEnabled_WhenDefault()
        {
            await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();
            Assert.True(_exposedTestViewModel.IsOptionsBannerEnabled);
            Assert.True(_exposedTestViewModel.IsOldPublishExperienceEnabled);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesEnabled_WhenNull()
        {
            _publishContextFixture.PublishSettingsRepository.Setup(mock => mock.GetAsync())
                .ReturnsAsync((PublishSettings)null);

           await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();
           Assert.True(_exposedTestViewModel.IsOptionsBannerEnabled);
           Assert.True(_exposedTestViewModel.IsOldPublishExperienceEnabled);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesEnabled_WhenErrorRetrievingSettings()
        {
            _publishContextFixture.PublishSettingsRepository.Setup(mock => mock.GetAsync())
                .ThrowsAsync(new SettingsException("error", null));

           await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();

            Assert.True(_exposedTestViewModel.IsOptionsBannerEnabled);
            Assert.True(_exposedTestViewModel.IsOldPublishExperienceEnabled);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesDisabled()
        {
            _publishContextFixture.PublishSettingsRepository.Setup(mock => mock.GetAsync())
                .ReturnsAsync(new PublishSettings() {ShowPublishBanner = false, ShowOldPublishExperience = false});

            await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();

            Assert.False(_exposedTestViewModel.IsOptionsBannerEnabled);
            Assert.False(_exposedTestViewModel.IsOldPublishExperienceEnabled);
        }


        [Fact]
        public void HasValidationErrors_WithError()
        {
            _sut.ConfigurationDetails.Add(
                ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .WithSampleError()
                    .Build()
            );

            Assert.True(_sut.HasValidationErrors());
        }

        [Fact]
        public void HasValidationErrors_WithNoError()
        {
            _sut.ConfigurationDetails = _sampleConfigurationDetails;

            Assert.False(_sut.HasValidationErrors());
        }

        [Fact]
        public void HasValidationErrors_WithNoDetails()
        {
            _sut.ConfigurationDetails = null;

            Assert.False(_sut.HasValidationErrors());
        }

        [Fact]
        public void HasValidationErrors_WithNestedError()
        {
            _sut.ConfigurationDetails.Add(
                ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithChild(ConfigurationDetailBuilder.Create()
                            .WithSampleError()
                        )
                    )
                    .Build()
            );

            Assert.True(_sut.HasValidationErrors());
        }

        [StaFact]
        public async Task PublishApplication()
        {
            await SetupPublishView();

            StubGetDeploymentStatus(DeploymentResultSuccess);

            await _sut.PublishApplication();

            Assert.Equal(_sampleApplicationName, _sut.StackName);
            Assert.Equal(ProgressStatus.Success, _sut.ProgressStatus);
            AssertPublishCallsAreCorrect();
            VerifyNoErrorCodeEmitted();
        }

        [StaFact]
        public async Task PublishApplication_Failed()
        {
            await SetupPublishView();

            StubGetDeploymentStatus(DeploymentResultFail);

            await _sut.PublishApplication();

            Assert.Equal(ProgressStatus.Fail, _sut.ProgressStatus);
            Assert.Contains(DeploymentResultFail.Exception.Message, _sut.PublishProgress);
            AssertPublishCallsAreCorrect();
            VerifyErrorCodeEmitted();
        }

        private void StubGetDeploymentStatus(GetDeploymentStatusOutput statusOutput)
        {
            _deployToolController.SetupSequence(mock => mock.GetDeploymentStatusAsync(_sampleSessionId))
                .Returns(Task.FromResult(DeploymentResultExecuting))
                .Returns(Task.FromResult(statusOutput))
                .Returns(Task.FromResult(statusOutput));
        }

        private void AssertPublishCallsAreCorrect()
        {
            AssertStartDeploymentCalledTimes(1);
            AssertGetDeploymentCalledTimes(3);
        }

        private void AssertStartDeploymentCalledTimes(int times)
        {
            _deployToolController.Verify(mock => mock.StartDeploymentAsync(_sampleSessionId), Times.Exactly(times));
        }

        private void AssertGetDeploymentCalledTimes(int times)
        {
            _deployToolController.Verify(mock => mock.GetDeploymentStatusAsync(_sampleSessionId), Times.Exactly(times));
        }

        [Fact]
        public async Task PublishApplication_WhenRepublish()
        {
            await SetupRepublishView();

            StubGetDeploymentStatus(DeploymentResultSuccess);

            await _sut.PublishApplication();

            AssertPublishCallsAreCorrect();
        }

        [Fact]
        public async Task PublishApplication_WhenExceptionThrown()
        {
            await SetupPublishView();

            _deployToolController.Setup(mock =>
                    mock.StartDeploymentAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("error"));

            await Assert.ThrowsAsync<Exception>(async () => await _sut.PublishApplication());

            Assert.Equal(_sampleApplicationName, _sut.StackName);
            AssertStartDeploymentCalledTimes(1);
            AssertGetDeploymentCalledTimes(0);
        }

        [Fact]
        public async Task StartDeploymentSession()
        {
            await _sut.StartDeploymentSession(_cancelToken);

            Assert.Equal(_sampleSessionId, _sut.SessionId);
            Assert.Equal(_sampleApplicationName, _sut.PublishStackName);
        }

        [Fact]
        public async Task StartDeploymentSession_RequestThrows()
        {
            _deployToolController.Setup(mock => mock.StartSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));

            await Assert.ThrowsAsync<SessionException>(async () => await _sut.StartDeploymentSession(_cancelToken));

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task StopDeploymentSession()
        {
            await _sut.StartDeploymentSession(_cancelToken);
            await _sut.StopDeploymentSession(_cancelToken);

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task StopDeploymentSession_NoSession()
        {
            Assert.Null(_sut.SessionId);

            await _sut.StopDeploymentSession(_cancelToken);

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task StopDeploymentSession_RequestThrows()
        {
            _deployToolController.Setup(mock => mock.StopSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));

            await _sut.StartDeploymentSession(_cancelToken);
            await Assert.ThrowsAsync<SessionException>(async () => await _sut.StopDeploymentSession(_cancelToken));

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task RestartDeploymentSessionAsync()
        {
            _sut.Recommendations = new ObservableCollection<PublishRecommendation>(_sampleRecommendations);
            _sut.RepublishTargets = new ObservableCollection<RepublishTarget>(_sampleRepublishTargets);

            var sampleSessionDetails = CreateSampleSessionDetails();
            sampleSessionDetails.SessionId = Guid.NewGuid().ToString();
            StubStartSessionToReturn(sampleSessionDetails);

            await _sut.RestartDeploymentSessionAsync(_cancelToken);

            Assert.Equal(sampleSessionDetails.SessionId, _sut.SessionId);
            Assert.Empty(_sut.Recommendations);
            Assert.Empty(_sut.RepublishTargets);
        }

        [Fact]
        public async Task RefreshRecommendations()
        {
            // arrange.
            await _sut.StartDeploymentSession(_cancelToken);

            // act.
            await _sut.RefreshRecommendations(_cancelToken);

            // assert.
            Assert.Equal(_sampleRecommendations, _sut.Recommendations);

            // The first recommendation is "selected" and the most recommended.
            Assert.True(_sut.Recommendation.IsRecommended);
            Assert.Equal(_sampleRecommendations.First(), _sut.Recommendation);
        }

        [Fact]
        public async Task RefreshRecommendations_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshRecommendations(_cancelToken);
            });

            Assert.Empty(_sut.Recommendations);
        }

        [Fact]
        public async Task RefreshRecommendations_RequestThrows()
        {
            _deployToolController.Setup(mock =>
                    mock.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));
            await _sut.StartDeploymentSession(_cancelToken);

            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshRecommendations(_cancelToken);
            });
            Assert.Empty(_sut.Recommendations);
        }

        [Fact]
        public async Task RefreshExistingTargets()
        {
            // arrange.
            await _sut.StartDeploymentSession(_cancelToken);

            // act.
            await _sut.RefreshExistingTargets(_cancelToken);

            // assert.
            Assert.Equal(_sampleRepublishTargets, _sut.RepublishTargets);

            // The first target is "selected"
            Assert.Equal(_sampleRepublishTargets.First(), _sut.RepublishTarget);
        }

        [Fact]
        public async Task RefreshExistingTargets_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshExistingTargets(_cancelToken);
            });
            Assert.Empty(_sut.RepublishTargets);
        }

        [Fact]
        public async Task RefreshExistingTargets_RequestThrows()
        {
            _deployToolController.Setup(mock =>
                    mock.GetRepublishTargetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));
            await _sut.StartDeploymentSession(_cancelToken);

            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshExistingTargets(_cancelToken);
            });
            Assert.Empty(_sut.RepublishTargets);
        }

        [Fact]
        public async Task UpdateSummaryAsync()
        {
            await SetupPublishView();
            await _sut.UpdateSummaryAsync(_cancelToken);

            Assert.Equal(_sut.GeneratePublishSummary(), _sut.PublishSummary);
        }
        
        [Fact]
        public async Task GeneratePublishSummary_WithTrueBool()
        {
            await SetupPublishView();
            _sut.ConfigurationDetails.Add(new ConfigurationDetail()
            {
                Name = "Use Vpc",
                Type = typeof(bool),
                Value = true,
                Visible = true,
            });

            var summary = _sut.GeneratePublishSummary();

            Assert.Contains("Use Vpc", summary);
        }

        [Fact]
        public async Task GeneratePublishSummary_WithFalseBool()
        {
            await SetupPublishView();
            _sut.ConfigurationDetails.Add(new ConfigurationDetail()
            {
                Name = "Use Vpc",
                Type = typeof(bool),
                Value = false,
                Visible = true,
            });

            var summary = _sut.GeneratePublishSummary();

            Assert.Contains("Use Vpc: False", summary);
        }

        [Fact]
        public async Task GeneratePublishSummary_NewPublish()
        {
            await SetupPublishView();

            Assert.Empty(_sut.GeneratePublishSummary());
        }

        [Fact]
        public async Task GeneratePublishSummary_Republish()
        {
            await SetupRepublishView();

            Assert.Empty(_sut.GeneratePublishSummary());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GeneratePublishSummary_ConfigurationDetails(bool isRepublish)
        {
            _sut.IsRepublish = isRepublish;
            var newConfigDetails = CreateSampleConfigurationDetails(2);
            _sut.ConfigurationDetails = newConfigDetails;

            var summary = _sut.GeneratePublishSummary();

            foreach (var detail in newConfigDetails)
            {
                Assert.Contains($"{detail.Name}: {detail.Value}", summary);
            }
        }

        [Fact]
        public void GeneratePublishSummary_NonVisibleConfigurationDetails()
        {
            _sut.IsRepublish = false;
            var newConfigDetails = CreateSampleConfigurationDetails(2, d => d.Visible = false);
            _sut.ConfigurationDetails = newConfigDetails;

            var summary = _sut.GeneratePublishSummary();

            foreach (var detail in newConfigDetails)
            {
                Assert.DoesNotContain($"{detail.Name}: {detail.Value}", summary);
            }
        }

        [Fact]
        public void GeneratePublishSummary_AdvancedConfigurationDetails()
        {
            _sut.IsRepublish = false;
            var newConfigDetails = CreateSampleConfigurationDetails(2, d => d.Advanced = true);
            _sut.ConfigurationDetails = newConfigDetails;

            var summary = _sut.GeneratePublishSummary();

            foreach (var detail in newConfigDetails)
            {
                Assert.DoesNotContain($"{detail.Name}: {detail.Value}", summary);
            }
        }

        [Fact]
        public void GeneratePublishSummary_NonSummaryDisplayableConfigurationDetails()
        {
            _sut.IsRepublish = true;
            var newConfigDetails = CreateSampleConfigurationDetails(2, d => d.SummaryDisplayable = false);
            _sut.ConfigurationDetails = newConfigDetails;

            var summary = _sut.GeneratePublishSummary();

            foreach (var detail in newConfigDetails)
            {
                Assert.DoesNotContain($"{detail.Name}: {detail.Value}", summary);
            }
        }

        [Theory]
        [InlineData(true, "#summaryvpc")]
        [InlineData(false, "create-new-vpc")]
        public void GeneratePublishSummary_NestedConfigurationDetails(bool isRepublish, string expectedChildDetail)
        {
            _sut.IsRepublish = isRepublish;
            CreateSampleSummaryConfigurationDetail();

            var summary = _sut.GeneratePublishSummary();

            Assert.Contains(expectedChildDetail, summary);
            Assert.Contains("use vpc:", summary);
            Assert.DoesNotContain("#novpc", summary);
            Assert.DoesNotContain("holograms", summary);
        }

        [Fact]
        public async Task JoinSession()
        {
            await _sut.StartDeploymentSession(_cancelToken);
            await _sut.JoinDeploymentSession();

            _deployClient.Verify(mock => mock.JoinSession(_sampleSessionId), Times.Once);
        }

        [Fact]
        public async Task JoinSession_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _sut.JoinDeploymentSession();
            });

            _deployClient.Verify(mock => mock.JoinSession(_sampleSessionId), Times.Never);
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenInitialPublish()
        {
            await _sut.UpdateRequiredPublishProperties(_cancelToken);

            Assert.True(string.IsNullOrEmpty(_sut.StackName));
            Assert.True(string.IsNullOrEmpty(_sut.TargetRecipe));
            Assert.True(string.IsNullOrEmpty(_sut.TargetDescription));
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenInitialRepublish()
        {
            _sut.IsRepublish = true;
            await _sut.UpdateRequiredPublishProperties(_cancelToken);

            AssertRequiredPublishProperties("","", "");
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenPublish()
        {
            await SetupPublishView();
            _sut.StackName = "abc";

            AssertRequiredPublishProperties("abc", _sut.Recommendation.Name, _sut.Recommendation.Description);
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenRepublish()
        {
            await SetupRepublishView();

            AssertRequiredPublishProperties(_sut.RepublishTarget.Name, _sut.RepublishTarget.RecipeName, _sut.RepublishTarget.Description);
        }

        [Fact]
        public async Task SetDeploymentTarget()
        {
            _deployToolController.Setup(mock =>
                    mock.SetDeploymentTarget(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));

            await SetupPublishView();

            _deployToolController.Verify(mock => mock.SetDeploymentTarget(_sampleSessionId, _sut.StackName, _sut.Recommendation.RecipeId, _sut.IsRepublish, _cancelToken), Times.Once);
        }

        [Fact]
        public async Task SetDeploymentTarget_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () => await _sut.SetDeploymentTarget(_cancelToken));
        }

        [Fact]
        public async Task SetDeploymentTarget_NoRecommendation()
        {
            await SetupPublishView();
            _sut.Recommendation = null;

            await _sut.SetDeploymentTarget(_cancelToken);

            _deployToolController.Verify(mock => mock.SetDeploymentTarget(_sampleSessionId, _sut.StackName, null, false,
                _cancelToken), Times.Once);
        }

        [Fact]
        public async Task SetDeploymentTarget_NoRepublish()
        {
            await SetupRepublishView();
            _sut.RepublishTarget = null;

            await _sut.SetDeploymentTarget(_cancelToken);

            _deployToolController.Verify(mock => mock.SetDeploymentTarget(_sampleSessionId, _sut.StackName, null, true,
                _cancelToken), Times.Once);
        }

        [Fact]
        public async Task RefreshTargetConfigurations()
        {
            await SetupPublishView();

            _deployToolController.Setup(mock =>
                    mock.GetConfigSettings(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_sampleConfigurationDetails);

            await _sut.RefreshTargetConfigurations(_cancelToken);

            _deployToolController.Verify(mock => mock.GetConfigSettings(_sampleSessionId, _cancelToken), Times.Once);
            Assert.Single(_sut.ConfigurationDetails);
        }

        [Fact]
        public async Task RefreshTargetConfigurations_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshTargetConfigurations(_cancelToken));
        }

        [Fact]
        public async Task RefreshTargetConfigurations_ThrowsException()
        {
            await SetupPublishView();

            _deployToolController.Setup(mock =>
                    mock.GetConfigSettings(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));

            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshTargetConfigurations(_cancelToken));
            Assert.Empty(_sut.ConfigurationDetails);
        }

        [Fact]
        public async Task SetTargetConfiguration()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                mock.ApplyConfigSettings(It.IsAny<string>(), It.IsAny<ConfigurationDetail>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(_sampleSetOptionSettingOutput);

            var response = await _sut.SetTargetConfiguration(_sampleConfigurationDetails[0], _cancelToken);

            _deployToolController.Verify(
                mock => mock.ApplyConfigSettings(_sampleSessionId, _sampleConfigurationDetails[0],
                    _cancelToken), Times.Once);
            Assert.Empty(response);
        }

        [Fact]
        public async Task SetTargetConfiguration_NoSession()
        {
            var response = await _sut.SetTargetConfiguration(_sampleConfigurationDetails[0], _cancelToken);

            Assert.True(!string.IsNullOrWhiteSpace(response));
        }

        [Fact]
        public async Task SetTargetConfiguration_ThrowsException()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                    mock.ApplyConfigSettings(It.IsAny<string>(), It.IsAny<ConfigurationDetail>(),
                        It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));

            var response = await _sut.SetTargetConfiguration(_sampleConfigurationDetails[0], _cancelToken);

            Assert.True(!string.IsNullOrWhiteSpace(response));
            Assert.Equal("simulated service error", response);
        }

        [Fact]
        public async Task SetTargetConfiguration_FailedToSet()
        {
            await SetupPublishView();

            var configDetail = new ConfigurationDetail
            {
                Id = "id", Name = "Name", Parent = new ConfigurationDetail {Id = "parentid", Name = "Parent Name"}
            };

            var failedSetOptionSettingOutput = new ApplyConfigSettingsOutput
            {
                FailedConfigUpdates = new Dictionary<string, string>() {{"parentid.id", "sample failure message"}}
            };
            _deployToolController.Setup(mock =>
                mock.ApplyConfigSettings(It.IsAny<string>(), It.IsAny<ConfigurationDetail>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(failedSetOptionSettingOutput);

            var response = await _sut.SetTargetConfiguration(configDetail, _cancelToken);

            _deployToolController.Verify(
                mock => mock.ApplyConfigSettings(_sampleSessionId, configDetail, _cancelToken),
                Times.Once);

            Assert.Equal("sample failure message", response);
        }

        [Fact]
        public async Task SetSystemCapabilities()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_sampleGetCompatibilityOutput);

            await _sut.RefreshSystemCapabilities(_cancelToken);

            _deployToolController.Verify(
                mock => mock.GetCompatibilityAsync(_sampleSessionId, _cancelToken), Times.Once);
            Assert.Empty(_sampleGetCompatibilityOutput);
        }

        [Fact]
        public async Task SetSystemCapabilities_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshSystemCapabilities(_cancelToken));
        }

        [Fact]
        public async Task SetSystemCapabilities_ThrowsException()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                    mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));

            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshSystemCapabilities(_cancelToken));
        }

        [Fact]
        public async Task RefreshSystemCapabilities_CapabilityInstalled()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_sampleGetCompatibilityOutput);

            Assert.Empty(_sut.MissingCapabilities.Missing);
            Assert.Empty(_sut.MissingCapabilities.Resolved);
        }

        [Fact]
        public async Task RefreshSystemCapabilities_CapabilityMissing()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateSampleSystemCapabilities);

            await _sut.RefreshSystemCapabilities(_cancelToken);

            Assert.Contains("Docker", _sut.MissingCapabilities.Missing);
            Assert.Empty(_sut.MissingCapabilities.Resolved);
        }

        private static List<TargetSystemCapability> CreateSampleSystemCapabilities()
        {
            var systemCapabilities = new List<TargetSystemCapability>();
            var summary = new SystemCapabilitySummary()
            {
                Name = "Docker", Message = "Please install Docker", Installed = false
            };
            var capability = new TargetSystemCapability(summary);
            systemCapabilities.Add(capability);
            return systemCapabilities;
        }

        [Fact]
        public async Task RefreshSystemCapabilities_CapabilityResolved()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock =>
                mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateSampleSystemCapabilities);

            await _sut.RefreshSystemCapabilities(_cancelToken);
            _deployToolController.Setup(mock =>
                mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_sampleGetCompatibilityOutput);
            await _sut.RefreshSystemCapabilities(_cancelToken);

            Assert.Contains("Docker", _sut.MissingCapabilities.Missing);
            Assert.Contains("Docker", _sut.MissingCapabilities.Resolved);
        }

        [Fact]
        public async Task ClearPublishedResources()
        {
            _sut.PublishedStackName = "foo";
            _sut.PublishResources =
                new ObservableCollection<PublishResource>() { new PublishResource("", "", "", null) };

            await _sut.ClearPublishedResources(_cancelToken);

            Assert.Empty(_sut.PublishedStackName);
            Assert.Empty(_sut.PublishResources);
        }

        [Fact]
        public async Task RefreshPublishResources()
        {
            await SetupPublishView();

            _deployToolController.Setup(mock =>
                mock.GetDeploymentDetails(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_deploymentDetails);

            await _sut.RefreshPublishedResources(_cancelToken);

            _deployToolController.Verify(mock => mock.GetDeploymentDetails(_sampleSessionId, _cancelToken), Times.Once);

            Assert.Single(_sut.PublishResources);
            Assert.Equal("sampleStack", _sut.PublishedStackName);
        }

        [Fact]
        public async Task RefreshPublishResources_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () => await _sut.RefreshPublishedResources(_cancelToken));
        }

        [Fact]
        public async Task RefreshPublishResources_ThrowsException()
        {
            await SetupPublishView();


            _deployToolController.Setup(mock =>
                mock.GetDeploymentDetails(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));

            await Assert.ThrowsAsync<Exception>(async () => await _sut.RefreshPublishedResources(_cancelToken));

            Assert.Empty(_sut.PublishResources);
            Assert.True(string.IsNullOrWhiteSpace(_sut.PublishedStackName));
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync()
        {
            await SetupForSetTargetConfigurations(_sampleSetOptionSettingOutput);

            var response = await _sut.ValidateTargetConfigurationsAsync();

            Assert.True(response);
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () => await _sut.ValidateTargetConfigurationsAsync());
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync_WhenExceptionThrown()
        {
            await SetupPublishView();
            _deployToolController.Setup(mock => mock.ApplyConfigSettings(It.IsAny<string>(),
                It.IsAny<IList<ConfigurationDetail>>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            await Assert.ThrowsAsync<Exception>(async () => await _sut.ValidateTargetConfigurationsAsync());
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync_WhenValidationError()
        {
            var problemDetail = _sampleConfigurationDetails[0];

            var settingsOutput = new ApplyConfigSettingsOutput()
            {
                FailedConfigUpdates = new Dictionary<string, string> { { problemDetail.GetLeafId(), "error" } }
            };
            await SetupForSetTargetConfigurations(settingsOutput);

            var response = await _sut.ValidateTargetConfigurationsAsync();

            Assert.False(response);
            Assert.Equal("error", problemDetail.ValidationMessage);
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync_WhenChildValidationError()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithId("parent")
                .WithType(typeof(object))
                .WithChild(ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .WithId("problem-child")
                    )
                .Build();

            _sampleConfigurationDetails.Add(detail);

            var problemChild = detail.Children.First();

            var settingsOutput = new ApplyConfigSettingsOutput()
            {
                FailedConfigUpdates = new Dictionary<string, string> { { problemChild.GetLeafId(), "child-error" } }
            };
            await SetupForSetTargetConfigurations(settingsOutput);

            var response = await _sut.ValidateTargetConfigurationsAsync();

            Assert.False(response);
            Assert.Equal("child-error", problemChild.ValidationMessage);
        }

        private async Task SetupForSetTargetConfigurations(ApplyConfigSettingsOutput settingsOutput)
        {
            await SetupPublishView();
            _sut.ConfigurationDetails = _sampleConfigurationDetails;
            _deployToolController.Setup(mock => mock.ApplyConfigSettings(It.IsAny<string>(),
                It.IsAny<IList<ConfigurationDetail>>(), It.IsAny<CancellationToken>())).ReturnsAsync(settingsOutput);
        }

        private static ObservableCollection<ConfigurationDetail> CreateSampleConfigurationDetails(int amount,
            Action<ConfigurationDetail> detailAdjustor = null)
        {
            var configurationDetails = new ObservableCollection<ConfigurationDetail>();
            var batch = Guid.NewGuid().ToString();

            for (int i = 0; i < amount; i++)
            {
                var name = Guid.NewGuid().ToString();

                var detail = new ConfigurationDetail
                {
                    Id = $"{batch}-{i}",
                    Name = name,
                    Description = $"Description for {name}",
                    Visible = true,
                    ReadOnly = false,
                    Advanced = false,
                    SummaryDisplayable = true,
                    DefaultValue = "test",
                    Value = "test",
                    Type = typeof(string)
                };

                detailAdjustor?.Invoke(detail);

                configurationDetails.Add(detail);
            }

            return configurationDetails;
        }

        private GetDeploymentDetailsOutput CreateSampleDeploymentDetails()
        {
            var resources = new List<DisplayedResourceSummary>()
            {
                new DisplayedResourceSummary()
                {
                    Id = "test-dev",
                    Type = "AWS::ElasticBeanstalk::Environment",
                    Description = "Application Endpoint",
                    Data = new Dictionary<string, string>() {{"Endpoint", "http://test-dev-endpoint.com/"}}
                }
            };
            var output = new GetDeploymentDetailsOutput() { StackId = "sampleStack", DisplayedResources = resources };
            return output;
        }

        private async Task SetupPublishView()
        {
            await _sut.StartDeploymentSession(_cancelToken);
            await _sut.RefreshRecommendations(_cancelToken);
            await _sut.UpdateRequiredPublishProperties(_cancelToken);
            await _sut.SetDeploymentTarget(_cancelToken);
        }

        private async Task SetupRepublishView()
        {
            _sut.IsRepublish = true;
            await _sut.StartDeploymentSession(_cancelToken);
            await _sut.RefreshExistingTargets(_cancelToken);
            await _sut.UpdateRequiredPublishProperties(_cancelToken);
            await _sut.SetDeploymentTarget(_cancelToken);
        }

        private void AssertRequiredPublishProperties(string stackName, string targetRecipe, string expectedRecommendationDescription)
        {
            Assert.Equal(stackName, _sut.StackName);
            Assert.Equal(targetRecipe, _sut.TargetRecipe);
            Assert.Equal(expectedRecommendationDescription, _sut.TargetDescription);
        }

        private void VerifyErrorCodeEmitted()
        {
            _publishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(p => p.Metadata.Values.Contains(DeploymentResultFail.Exception.ErrorCode)))),
                Times.Once);
        }

        private void VerifyNoErrorCodeEmitted()
        {
            _publishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(p => p.Metadata.Keys.Contains("errorCode")))), Times.Never);
        }

        private void CreateSampleSummaryConfigurationDetail()
        {
            _sut.ConfigurationDetails.Add(
                ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .IsVisible()
                    .IsSummaryDisplayable()
                    .WithName("use vpc")
                    .WithType(typeof(object))
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("not-visible-or-advanced #novpc")
                    )
                    // (create-new-vpc) is the only child of (use vpc) that should be returned
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsVisible()
                        .WithName("create-new-vpc")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsAdvanced()
                        .WithName("advanced-not-visible #novpc")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsVisible()
                        .IsAdvanced()
                        .WithName("visible-and-advanced #novpc")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("summary displayable #summaryvpc")
                        .IsSummaryDisplayable())
                    .Build()
            );

            _sut.ConfigurationDetails.Add(
                // This node and its children should be excluded (not visible)
                ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .WithType(typeof(object))
                    .WithName("use holograms")
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("holograms: not-visible-or-advanced")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsSummaryDisplayable()
                        .WithName("holograms: summary displayable")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsVisible()
                        .WithName("holograms: visible-not-advanced")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsAdvanced()
                        .WithName("holograms: advanced-not-visible")
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .IsVisible()
                        .IsAdvanced()
                        .WithName("holograms: visible-and-advanced")
                    )
                    .Build()
            );
        }

    }
}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
