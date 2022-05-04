using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Publishing.Common;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class PublishToAwsDocumentViewModelTests
    {
        private readonly PublishToAwsFixture _publishToAwsFixture = new PublishToAwsFixture();

        private DeployToolControllerFixture DeployToolControllerFixture =>
            _publishToAwsFixture.DeployToolControllerFixture;
        private PublishContextFixture PublishContextFixture => _publishToAwsFixture.PublishContextFixture;

        private readonly Mock<IDeploymentCommunicationClient> _deployClient = new Mock<IDeploymentCommunicationClient>();
        private readonly PublishToAwsDocumentViewModel _sut;
        private readonly TestPublishToAwsDocumentViewModel _exposedTestViewModel;

        private readonly List<TargetSystemCapability> _sampleGetCompatibilityOutput =
            new List<TargetSystemCapability>();

        private List<PublishRecommendation> SamplePublishRecommendations => DeployToolControllerFixture.GetRecommendationsAsyncResponse;

        private List<RepublishTarget> SampleRepublishTargets => DeployToolControllerFixture.GetRepublishTargetsAsyncResponse;

        private List<ConfigurationDetail> SampleConfigurationDetails => DeployToolControllerFixture.GetConfigSettingsAsyncResponse;

        private static readonly ICredentialIdentifier SampleCredentialIdentifier =
            new SharedCredentialIdentifier("profile");

        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion()
        {
            PartitionId = "SamplePartition", DisplayName = "SampleRegion", Id = "region1",
        };

        private static readonly ToolkitRegion SampleFallbackRegion = new ToolkitRegion()
        {
            PartitionId = "SamplePartition", DisplayName = "FallbackRegion", Id = RegionEndpoint.USEast1.SystemName,
        };

        private CancellationToken CancelToken => PublishContextFixture.PublishContext.PublishPackage.DisposalToken;

        public PublishToAwsDocumentViewModelTests()
        {
            PublishContextFixture.DefineRegion(_sampleRegion);
            PublishContextFixture.DefineRegion(SampleFallbackRegion);

            _sut = new PublishToAwsDocumentViewModel(_publishToAwsFixture.PublishApplicationContext)
            {
                DeploymentClient = _deployClient.Object, DeployToolController = DeployToolControllerFixture.DeployToolController.Object
            };
            _sut.Connection.Region = _sampleRegion;

            _exposedTestViewModel = new TestPublishToAwsDocumentViewModel(_publishToAwsFixture.PublishApplicationContext);

            DeployToolControllerFixture.GetCompatibilityAsyncResponse = _sampleGetCompatibilityOutput;
        }

        [Fact]
        public async Task ShouldInitializeAsRepublish()
        {
            //arrange
            await _sut.StartDeploymentSessionAsync(CancelToken);
            //act
            await _sut.InitializePublishTargetsAsync(CancelToken);
            //assert
            Assert.True(_sut.RepublishTargets.Any());
            Assert.True(_sut.IsRepublish);
        }

        [Fact]
        public async Task ShouldInitializeAsPublish()
        {
            //arrange
            await _sut.StartDeploymentSessionAsync(CancelToken);
            SampleRepublishTargets.Clear();
            //act
            await _sut.InitializePublishTargetsAsync(CancelToken);
            //assert
            Assert.False(_sut.RepublishTargets.Any());
            Assert.False(_sut.IsRepublish);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesEnabled_WhenDefault()
        {
            await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();
            Assert.True(_exposedTestViewModel.IsOptionsBannerEnabled);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesEnabled_WhenNull()
        {
            PublishContextFixture.PublishSettingsRepository.Setup(mock => mock.GetAsync())
                .ReturnsAsync((PublishSettings)null);

           await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();
           Assert.True(_exposedTestViewModel.IsOptionsBannerEnabled);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesEnabled_WhenErrorRetrievingSettings()
        {
            PublishContextFixture.PublishSettingsRepository.Setup(mock => mock.GetAsync())
                .ThrowsAsync(new SettingsException("error", null));

           await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();

            Assert.True(_exposedTestViewModel.IsOptionsBannerEnabled);
        }

        [Fact]
        public async Task HasOptionsButtonPropertiesDisabled()
        {
            PublishContextFixture.PublishSettingsRepository.Setup(mock => mock.GetAsync())
                .ReturnsAsync(new PublishSettings() {ShowPublishBanner = false});

            await _exposedTestViewModel.ExposedLoadOptionsButtonSettingsAsync();

            Assert.False(_exposedTestViewModel.IsOptionsBannerEnabled);
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
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(SampleConfigurationDetails);

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
            await _sut.UpdatePublishProjectViewModelAsync();

            StubGetDeploymentStatus(SamplePublishData.GetDeploymentStatusOutputs.Success);

            await _sut.PublishApplicationAsync();

            Assert.Equal(PublishToAwsFixture.SampleApplicationName, _sut.StackName);
            Assert.Equal(ProgressStatus.Success, _sut.PublishProjectViewModel.ProgressStatus);
            AssertPublishCallsAreCorrect();
            VerifyNoErrorCodeEmitted();
        }

        [StaFact]
        public async Task PublishApplication_Failed()
        {
            await SetupPublishView();
            await _sut.UpdatePublishProjectViewModelAsync();

            StubGetDeploymentStatus(SamplePublishData.GetDeploymentStatusOutputs.Fail);

            await _sut.PublishApplicationAsync();

            Assert.Equal(ProgressStatus.Fail, _sut.PublishProjectViewModel.ProgressStatus);
            Assert.Contains(SamplePublishData.GetDeploymentStatusOutputs.Fail.Exception.Message, _sut.PublishProjectViewModel.PublishProgress);
            AssertPublishCallsAreCorrect();
            VerifyErrorCodeEmitted();
        }

        private void StubGetDeploymentStatus(GetDeploymentStatusOutput statusOutput)
        {
            DeployToolControllerFixture.SetupGetDeploymentStatusAsync(SamplePublishData.GetDeploymentStatusOutputs.Executing, statusOutput, statusOutput);
        }

        private void AssertPublishCallsAreCorrect()
        {
            DeployToolControllerFixture.AssertStartDeploymentCalledTimes(1);
            DeployToolControllerFixture.AssertGetDeploymentCalledTimes(3);
        }

        [Fact]
        public async Task PublishApplication_WhenRepublish()
        {
            await SetupRepublishView();
            await _sut.UpdatePublishProjectViewModelAsync();

            StubGetDeploymentStatus(SamplePublishData.GetDeploymentStatusOutputs.Success);

            await _sut.PublishApplicationAsync();

            AssertPublishCallsAreCorrect();
        }

        [Fact]
        public async Task PublishApplication_WhenExceptionThrown()
        {
            await SetupPublishView();
            await _sut.UpdatePublishProjectViewModelAsync();

            DeployToolControllerFixture.StubStartDeploymentAsyncThrows("service failure");

            var result = await _sut.PublishApplicationAsync();

            Assert.False(result.IsSuccess);
            Assert.Contains("service failure", result.ErrorMessage);
            Assert.Equal(PublishToAwsFixture.SampleApplicationName, _sut.StackName);
            DeployToolControllerFixture.AssertStartDeploymentCalledTimes(1);
            DeployToolControllerFixture.AssertGetDeploymentCalledTimes(0);
        }

        [Fact]
        public async Task EmitMetricsForPublishProjectAsync()
        {
            var publishResult = new PublishProjectResult() { IsSuccess = true };

            var result = await _sut.EmitMetricsForPublishProjectAsync(() => Task.FromResult(publishResult));

            Assert.Equal(publishResult, result);

            _publishToAwsFixture.PublishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(metricDatum => metricDatum.Metadata.Keys.Contains("result") &&
                                              metricDatum.Metadata["result"] == Result.Failed.ToString()))),
                Times.Never);
        }

        [Fact]
        public async Task EmitMetricsForPublishProjectAsync_PublishFailure()
        {
            var publishResult = new PublishProjectResult() { IsSuccess = false, ErrorCode = "error-code" };

            var result = await _sut.EmitMetricsForPublishProjectAsync(() => Task.FromResult(publishResult));

            Assert.Equal(publishResult, result);

            _publishToAwsFixture.PublishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(metricDatum => metricDatum.Metadata.Values.Contains(publishResult.ErrorCode)))),
                Times.Once);
        }

        [Fact]
        public async Task AdjustUiForPublishProjectAsync()
        {
            var publishResult = new PublishProjectResult() { IsSuccess = true };

            var result = await _sut.AdjustUiForPublishProjectAsync(() => Task.FromResult(publishResult));

            Assert.Equal(publishResult, result);
            Assert.Equal(ProgressStatus.Success, _sut.PublishProjectViewModel.ProgressStatus);
            Assert.Contains("was published as", _sut.PublishProjectViewModel.PublishProgress);
            Assert.False(_sut.PublishProjectViewModel.IsFailureBannerEnabled);
        }

        [Fact]
        public async Task AdjustUiForPublishProjectAsync_PublishFailed()
        {
            var publishResult = new PublishProjectResult() { IsSuccess = false, ErrorMessage = "some error message" };

            var result = await _sut.AdjustUiForPublishProjectAsync(() => Task.FromResult(publishResult));

            Assert.Equal(publishResult, result);

            Assert.Equal(ProgressStatus.Fail, _sut.PublishProjectViewModel.ProgressStatus);
            Assert.Contains(publishResult.ErrorMessage, _sut.PublishProjectViewModel.PublishProgress);
            Assert.True(_sut.PublishProjectViewModel.IsFailureBannerEnabled);
        }

        [Fact]
        public async Task StartDeploymentSession()
        {
            await _sut.StartDeploymentSessionAsync(CancelToken);

            Assert.Equal(PublishToAwsFixture.SampleSessionId, _sut.SessionId);
            Assert.Equal(PublishToAwsFixture.SampleApplicationName, _sut.PublishStackName);
        }

        [Fact]
        public async Task StartDeploymentSession_RequestThrows()
        {
            DeployToolControllerFixture.StubStartSessionAsyncThrows();

            await Assert.ThrowsAsync<SessionException>(async () => await _sut.StartDeploymentSessionAsync(CancelToken));

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task StopDeploymentSession()
        {
            await _sut.StartDeploymentSessionAsync(CancelToken);
            await _sut.StopDeploymentSessionAsync(CancelToken);

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task StopDeploymentSession_NoSession()
        {
            Assert.Null(_sut.SessionId);

            await _sut.StopDeploymentSessionAsync(CancelToken);

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task StopDeploymentSession_RequestThrows()
        {
            DeployToolControllerFixture.StubStopSessionAsyncThrows();

            await _sut.StartDeploymentSessionAsync(CancelToken);
            await Assert.ThrowsAsync<SessionException>(async () => await _sut.StopDeploymentSessionAsync(CancelToken));

            Assert.Null(_sut.SessionId);
        }

        [Fact]
        public async Task RestartDeploymentSessionAsync()
        {
            _sut.Recommendations = new ObservableCollection<PublishRecommendation>(SamplePublishRecommendations);
            _sut.RepublishTargets = new ObservableCollection<RepublishTarget>(SampleRepublishTargets);

            DeployToolControllerFixture.StartSessionAsyncResponse.SessionId = Guid.NewGuid().ToString();

            await _sut.RestartDeploymentSessionAsync(CancelToken);

            Assert.Equal(DeployToolControllerFixture.StartSessionAsyncResponse.SessionId, _sut.SessionId);
            Assert.Empty(_sut.Recommendations);
            Assert.Empty(_sut.RepublishTargets);
        }

        [Fact]
        public async Task RefreshRecommendations()
        {
            // arrange.
            await _sut.StartDeploymentSessionAsync(CancelToken);

            // act.
            await _sut.RefreshRecommendationsAsync(CancelToken);

            // assert.
            Assert.Equal(SamplePublishRecommendations, _sut.Recommendations);

            // The first recommendation is "selected" and the most recommended.
            var newPublishTarget = Assert.IsType<PublishRecommendation>(_sut.PublishDestination);
            Assert.True(newPublishTarget.IsRecommended);
            Assert.Equal(SamplePublishRecommendations.First(), newPublishTarget);
        }

        [Fact]
        public async Task RefreshRecommendations_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshRecommendationsAsync(CancelToken);
            });

            Assert.Empty(_sut.Recommendations);
        }

        [Fact]
        public async Task RefreshRecommendations_RequestThrows()
        {
            DeployToolControllerFixture.StubGetRecommendationsAsyncThrows();
            await _sut.StartDeploymentSessionAsync(CancelToken);

            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshRecommendationsAsync(CancelToken);
            });
            Assert.Empty(_sut.Recommendations);
        }

        [Fact]
        public async Task RefreshExistingTargets()
        {
            // arrange.
            _sut.IsRepublish = true;
            await _sut.StartDeploymentSessionAsync(CancelToken);

            // act.
            await _sut.RefreshExistingTargetsAsync(CancelToken);

            // assert.
            Assert.Equal(SampleRepublishTargets, _sut.RepublishTargets);

            // The first target is "selected"
            Assert.Equal(SampleRepublishTargets.First(), _sut.PublishDestination);
        }

        [Fact]
        public async Task RefreshExistingTargets_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshExistingTargetsAsync(CancelToken);
            });
            Assert.Empty(_sut.RepublishTargets);
        }

        [Fact]
        public async Task RefreshExistingTargets_RequestThrows()
        {
            DeployToolControllerFixture.StubGetRepublishTargetsAsyncThrows();
            await _sut.StartDeploymentSessionAsync(CancelToken);

            await Assert.ThrowsAsync<PublishException>(async () =>
            {
                await _sut.RefreshExistingTargetsAsync(CancelToken);
            });
            Assert.Empty(_sut.RepublishTargets);
        }

        [Fact]
        public async Task UpdateSummaryAsync()
        {
            await SetupPublishView();
            await _sut.UpdateSummaryAsync(CancelToken);

            Assert.Equal(_sut.GeneratePublishSummary(), _sut.PublishSummary);
        }
        
        [Fact]
        public async Task GeneratePublishSummary_WithTrueBool()
        {
            await SetupPublishView();
            _sut.ConfigurationDetails.Add(new ConfigurationDetail()
            {
                Name = "Use Vpc",
                Type = DetailType.Boolean,
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
                Type = DetailType.Boolean,
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
            var newConfigDetails = PublishToAwsFixture.CreateSampleConfigurationDetails(2);
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(newConfigDetails);

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
            var newConfigDetails = PublishToAwsFixture.CreateSampleConfigurationDetails(2, d => d.Visible = false);
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(newConfigDetails);

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
            var newConfigDetails = PublishToAwsFixture.CreateSampleConfigurationDetails(2, d => d.Advanced = true);
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(newConfigDetails);

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
            var newConfigDetails = PublishToAwsFixture.CreateSampleConfigurationDetails(2, d => d.SummaryDisplayable = false);
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(newConfigDetails);

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
            await _sut.StartDeploymentSessionAsync(CancelToken);
            await _sut.JoinDeploymentSessionAsync();

            _deployClient.Verify(mock => mock.JoinSession(DeployToolControllerFixture.SessionId), Times.Once);
        }

        [Fact]
        public async Task JoinSession_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _sut.JoinDeploymentSessionAsync();
            });

            _deployClient.Verify(mock => mock.JoinSession(DeployToolControllerFixture.SessionId), Times.Never);
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenInitialPublish()
        {
            await _sut.UpdateRequiredPublishPropertiesAsync(CancelToken);

            Assert.True(string.IsNullOrEmpty(_sut.StackName));
            Assert.True(string.IsNullOrEmpty(_sut.TargetRecipe));
            Assert.True(string.IsNullOrEmpty(_sut.TargetDescription));
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenInitialRepublish()
        {
            _sut.IsRepublish = true;
            await _sut.UpdateRequiredPublishPropertiesAsync(CancelToken);

            AssertRequiredPublishProperties("","", "");
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenPublish()
        {
            await SetupPublishView();
            _sut.StackName = "abc";

            AssertRequiredPublishProperties("abc", _sut.PublishDestination.Name, _sut.PublishDestination.Description);
        }

        [Fact]
        public async Task UpdateRequiredPublishProperties_WhenRepublish()
        {
            await SetupRepublishView();

            AssertRequiredPublishProperties(_sut.PublishDestination.Name, _sut.PublishDestination.RecipeName, _sut.PublishDestination.Description);
        }

        [Fact]
        public async Task SetDeploymentTarget_NewPublish()
        {
            await SetupPublishView();

            DeployToolControllerFixture.DeployToolController.Verify(NewPublishToSetDeploymentTargetAsyncWithCurrentState(), Times.Once);
        }

        private Expression<Func<IDeployToolController, Task>> NewPublishToSetDeploymentTargetAsyncWithCurrentState()
        {
            return mock => mock.SetDeploymentTargetAsync(DeployToolControllerFixture.SessionId, _sut.PublishDestination as PublishRecommendation, 
                _sut.StackName, CancelToken);
        }

        [Fact]
        public async Task SetDeploymentTarget_Republish()
        {
            await SetupRepublishView();

            DeployToolControllerFixture.DeployToolController.Verify(RepublishSetDeploymentTargetAsyncWithCurrentState(), Times.Once);
        }

        private Expression<Func<IDeployToolController, Task>> RepublishSetDeploymentTargetAsyncWithCurrentState()
        {
            return mock => mock.SetDeploymentTargetAsync(DeployToolControllerFixture.SessionId, _sut.PublishDestination as RepublishTarget, CancelToken);
        }

        [Fact]
        public async Task SetDeploymentTarget_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () => await _sut.SetDeploymentTargetAsync(CancelToken));
        }

        [Fact]
        public async Task SetDeploymentTarget_NoRecommendation()
        {
            await SetupPublishView();
            _sut.PublishDestination = null;

            await _sut.SetDeploymentTargetAsync(CancelToken);

            DeployToolControllerFixture.DeployToolController.Verify(NewPublishToSetDeploymentTargetAsyncWithCurrentState(), Times.Once);
        }

        [Fact]
        public async Task SetDeploymentTarget_NoRepublish()
        {
            await SetupRepublishView();
            _sut.PublishDestination = null;

            await _sut.SetDeploymentTargetAsync(CancelToken);

            DeployToolControllerFixture.DeployToolController.Verify(RepublishSetDeploymentTargetAsyncWithCurrentState(), Times.Once);
        }

        [Theory]
        [InlineData(DeploymentTypes.BeanstalkEnvironment, true)]
        [InlineData(DeploymentTypes.CloudFormationStack, true)]
        [InlineData(DeploymentTypes.ElasticContainerRegistryImage, false)]
        public void IsApplicationNameRequired(DeploymentTypes deploymentType, bool isApplicationNameRequired)
        {
            // Arrange
            var recommendation = new PublishRecommendation(new RecommendationSummary()
            {
                RecipeId = "sample-recipe", TargetService = "sample-service", DeploymentType = deploymentType
            });

            // Act
            _sut.PublishDestination = recommendation;

            // Assert
            Assert.Equal(isApplicationNameRequired, _sut.IsApplicationNameRequired);
        }

        [Fact]
        public async Task RefreshTargetConfigurations()
        {
            await SetupPublishView();

            await _sut.RefreshTargetConfigurationsAsync(CancelToken);

            DeployToolControllerFixture.AssertGetConfigSettingsAsyncCalledTimes(1);
            Assert.Single(_sut.ConfigurationDetails);
        }


        [Fact]
        public async Task RefreshTargetConfigurations_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshTargetConfigurationsAsync(CancelToken));
        }

        [Fact]
        public async Task RefreshTargetConfigurations_ThrowsException()
        {
            await SetupPublishView();

            DeployToolControllerFixture.StubGetConfigSettingsAsyncThrows();

            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshTargetConfigurationsAsync(CancelToken));
            Assert.Empty(_sut.ConfigurationDetails);
        }

        [Fact]
        public async Task RefreshTargetConfigurations_UpdatesUnsupportedSettingTypes()
        {
            await SetupPublishView();
            Assert.False(_sut.UnsupportedSettingTypes.RecipeToUnsupportedSetting.Any());

            SampleConfigurationDetails.Clear();
            SampleConfigurationDetails.AddRange(
                PublishToAwsFixture.CreateSampleConfigurationDetails(2, d => d.Type = DetailType.Unsupported));

            await _sut.RefreshTargetConfigurationsAsync(CancelToken);

            Assert.True(_sut.UnsupportedSettingTypes.RecipeToUnsupportedSetting.Any());
        }

        [Fact]
        public async Task RefreshConfigurationSettingValues()
        {
            await SetupPublishView();
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(SampleConfigurationDetails);

            Assert.False(_sut.ConfigurationDetails.First().HasValueMappings());

            var output = ApplyResourcesToConfigDetails(SampleConfigurationDetails);
            DeployToolControllerFixture.DeployToolController.Setup(mock =>
                mock.UpdateConfigSettingValuesAsync(It.IsAny<string>(), SampleConfigurationDetails, It.IsAny<CancellationToken>())).ReturnsAsync(output);

            await _sut.RefreshConfigurationSettingValuesAsync(CancelToken);

            DeployToolControllerFixture.DeployToolController.Verify(mock => mock.UpdateConfigSettingValuesAsync(DeployToolControllerFixture.SessionId, SampleConfigurationDetails,  CancelToken), Times.Once);
            Assert.True(_sut.ConfigurationDetails.First().ValueMappings.ContainsKey("abc"));
        }

        private List<ConfigurationDetail> ApplyResourcesToConfigDetails(IList<ConfigurationDetail> sampleConfigurationDetails)
        {
            sampleConfigurationDetails.ToList().ForEach(x => x.ValueMappings = new Dictionary<string, string>() {{"abc", "def"}});
            return sampleConfigurationDetails.ToList();
        }

        [Fact]
        public async Task RefreshConfigurationSettingValues_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshConfigurationSettingValuesAsync(CancelToken));
        }

        [Fact]
        public async Task RefreshConfigurationSettingValues_ThrowsException()
        {
            await SetupPublishView();

            DeployToolControllerFixture.DeployToolController.Setup(mock =>
                    mock.UpdateConfigSettingValuesAsync(It.IsAny<string>(), It.IsAny<List<ConfigurationDetail>>(),It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));

            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshConfigurationSettingValuesAsync(CancelToken));
            Assert.Empty(_sut.ConfigurationDetails);
        }

        [Fact]
        public async Task SetTargetConfiguration()
        {
            await SetupPublishView();

            var validation = await _sut.SetTargetConfigurationAsync(SampleConfigurationDetails[0], CancelToken);

            DeployToolControllerFixture.DeployToolController.Verify(
                mock => mock.ApplyConfigSettingsAsync(DeployToolControllerFixture.SessionId, SampleConfigurationDetails[0],
                    CancelToken), Times.Once);
            Assert.False(validation.HasErrors());
        }

        [Fact]
        public async Task SetTargetConfiguration_NoSession()
        {
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _sut.SetTargetConfigurationAsync(SampleConfigurationDetails[0], CancelToken);
            });
        }

        [Fact]
        public async Task SetTargetConfiguration_ThrowsException()
        {
            await SetupPublishView();
            DeployToolControllerFixture.StubApplyConfigSettingsAsyncThrows();

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _sut.SetTargetConfigurationAsync(SampleConfigurationDetails[0], CancelToken);
            });
        }

        [Fact]
        public async Task SetTargetConfiguration_FailedToSet()
        {
            await SetupPublishView();

            var configDetail = new ConfigurationDetail
            {
                Id = "id", Name = "Name", Parent = new ConfigurationDetail {Id = "parentid", Name = "Parent Name"}
            };

            var failedValidation = new ValidationResult();
            failedValidation.AddError("parentid.id", "sample failure message");
            DeployToolControllerFixture.ApplyConfigSettingsAsyncResponse = failedValidation;

            var validation = await _sut.SetTargetConfigurationAsync(configDetail, CancelToken);

            DeployToolControllerFixture.DeployToolController.Verify(
                mock => mock.ApplyConfigSettingsAsync(DeployToolControllerFixture.SessionId, configDetail, CancelToken),
                Times.Once);

            Assert.Equal("sample failure message", validation.GetError(validation.GetErrantDetailIds().First()));
        }

        [Fact]
        public async Task SetSystemCapabilities()
        {
            await SetupPublishView();

            await _sut.RefreshSystemCapabilitiesAsync(CancelToken);

            DeployToolControllerFixture.DeployToolController.Verify(
                mock => mock.GetCompatibilityAsync(DeployToolControllerFixture.SessionId, CancelToken), Times.Once);
            Assert.Empty(_sampleGetCompatibilityOutput);
        }

        [Fact]
        public async Task SetSystemCapabilities_NoSession()
        {
            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshSystemCapabilitiesAsync(CancelToken));
        }

        [Fact]
        public async Task SetSystemCapabilities_ThrowsException()
        {
            await SetupPublishView();
            DeployToolControllerFixture.StubGetCompatibilityAsyncThrows();

            await Assert.ThrowsAsync<PublishException>(async () => await _sut.RefreshSystemCapabilitiesAsync(CancelToken));
        }

        [Fact]
        public async Task RefreshSystemCapabilities_AdjustsLoadingSystemCapabilities()
        {
            var loadingAdjustments = new List<bool>();

            _sut.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_sut.LoadingSystemCapabilities))
                {
                    loadingAdjustments.Add(_sut.LoadingSystemCapabilities);
                }
            };

            await SetupPublishView();

            Assert.False(_sut.LoadingSystemCapabilities);
            await _sut.RefreshSystemCapabilitiesAsync(CancelToken);

            Assert.Equal(2, loadingAdjustments.Count);
            Assert.True(loadingAdjustments[0]);
            Assert.False(loadingAdjustments[1]);
        }

        [Fact]
        public async Task RefreshSystemCapabilities_CapabilityInstalled()
        {
            await SetupPublishView();

            await _sut.RefreshSystemCapabilitiesAsync(CancelToken);

            Assert.Empty(_sut.MissingCapabilities.Missing);
            Assert.Empty(_sut.MissingCapabilities.Resolved);
        }

        [Fact]
        public async Task RefreshSystemCapabilities_CapabilityMissing()
        {
            await SetupPublishView();
            DeployToolControllerFixture.GetCompatibilityAsyncResponse = CreateSampleSystemCapabilities();

            await _sut.RefreshSystemCapabilitiesAsync(CancelToken);

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
            DeployToolControllerFixture.GetCompatibilityAsyncResponse = CreateSampleSystemCapabilities();
            await _sut.RefreshSystemCapabilitiesAsync(CancelToken);
            DeployToolControllerFixture.GetCompatibilityAsyncResponse = _sampleGetCompatibilityOutput;

            await _sut.RefreshSystemCapabilitiesAsync(CancelToken);

            Assert.Contains("Docker", _sut.MissingCapabilities.Missing);
            Assert.Contains("Docker", _sut.MissingCapabilities.Resolved);
        }

        [Fact]
        public void CyclePublishDestination_NewToNew()
        {
            _exposedTestViewModel.SetPreviousPublishDestination(SamplePublishRecommendations[1]);
            _exposedTestViewModel.PublishDestination = SamplePublishRecommendations[0];
            _exposedTestViewModel.IsRepublish = false;

            _exposedTestViewModel.CyclePublishDestination();
            Assert.Equal(SamplePublishRecommendations[0], _exposedTestViewModel.PublishDestination);
        }

        [Fact]
        public void CyclePublishDestination_RepublishToNew()
        {
            _exposedTestViewModel.SetPreviousPublishDestination(SamplePublishRecommendations[0]);
            _exposedTestViewModel.PublishDestination = SampleRepublishTargets[0];
            _exposedTestViewModel.IsRepublish = false;

            _exposedTestViewModel.CyclePublishDestination();
            Assert.Equal(SamplePublishRecommendations[0], _exposedTestViewModel.PublishDestination);
        }

        [Fact]
        public void CyclePublishDestination_RepublishToRepublish()
        {
            _exposedTestViewModel.SetPreviousPublishDestination(SampleRepublishTargets[1]);
            _exposedTestViewModel.PublishDestination = SampleRepublishTargets[0];
            _exposedTestViewModel.IsRepublish = true;

            _exposedTestViewModel.CyclePublishDestination();
            Assert.Equal(SampleRepublishTargets[0], _exposedTestViewModel.PublishDestination);
        }

        [Fact]
        public void CyclePublishDestination_NewToRepublish()
        {
            _exposedTestViewModel.SetPreviousPublishDestination(SampleRepublishTargets[0]);
            _exposedTestViewModel.PublishDestination = SamplePublishRecommendations[0];
            _exposedTestViewModel.IsRepublish = true;

            _exposedTestViewModel.CyclePublishDestination();
            Assert.Equal(SampleRepublishTargets[0], _exposedTestViewModel.PublishDestination);
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync()
        {
            await SetupForSetTargetConfigurations();

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
            DeployToolControllerFixture.StubApplyConfigSettingsAsyncThrows();

            await Assert.ThrowsAsync<Exception>(async () => await _sut.ValidateTargetConfigurationsAsync());
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync_WhenValidationError()
        {
            var problemDetail = SampleConfigurationDetails[0];

            var validationResult = new ValidationResult();
            validationResult.AddError(problemDetail.GetLeafId(), "error");
            DeployToolControllerFixture.ApplyConfigSettingsAsyncResponse = validationResult;
            await SetupForSetTargetConfigurations();

            var response = await _sut.ValidateTargetConfigurationsAsync();

            Assert.False(response);
            Assert.Equal("error", problemDetail.ValidationMessage);
        }

        [Fact]
        public async Task ValidateTargetConfigurationsAsync_WhenChildValidationError()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithId("parent")
                .WithType(DetailType.Blob)
                .WithChild(ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .WithId("problem-child")
                    )
                .Build();

            SampleConfigurationDetails.Add(detail);

            var problemChild = detail.Children.First();

            var validationResult = new ValidationResult();
            validationResult.AddError(problemChild.GetLeafId(), "child-error");
            DeployToolControllerFixture.ApplyConfigSettingsAsyncResponse = validationResult;
            await SetupForSetTargetConfigurations();

            var response = await _sut.ValidateTargetConfigurationsAsync();

            Assert.False(response);
            Assert.Equal("child-error", problemChild.ValidationMessage);
        }

        private async Task SetupForSetTargetConfigurations()
        {
            await SetupPublishView();
            _sut.ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(SampleConfigurationDetails);
        }

        private async Task SetupPublishView()
        {
            await _sut.StartDeploymentSessionAsync(CancelToken);
            await _sut.RefreshRecommendationsAsync(CancelToken);
            await _sut.UpdateRequiredPublishPropertiesAsync(CancelToken);
            await _sut.SetDeploymentTargetAsync(CancelToken);
        }

        private async Task SetupRepublishView()
        {
            _sut.IsRepublish = true;
            await _sut.StartDeploymentSessionAsync(CancelToken);
            await _sut.RefreshExistingTargetsAsync(CancelToken);
            await _sut.UpdateRequiredPublishPropertiesAsync(CancelToken);
            await _sut.SetDeploymentTargetAsync(CancelToken);
        }

        private void AssertRequiredPublishProperties(string stackName, string targetRecipe, string expectedRecommendationDescription)
        {
            Assert.Equal(stackName, _sut.StackName);
            Assert.Equal(targetRecipe, _sut.TargetRecipe);
            Assert.Equal(expectedRecommendationDescription, _sut.TargetDescription);
        }

        private void VerifyErrorCodeEmitted()
        {
            PublishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(p => p.Metadata.Values.Contains(SamplePublishData.GetDeploymentStatusOutputs.Fail.Exception.ErrorCode)))),
                Times.Once);
        }

        private void VerifyNoErrorCodeEmitted()
        {
            PublishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.Is<Metrics>(m =>
                    m.Data.Any(p => p.Metadata.Keys.Contains("errorCode")))), Times.Never);
        }

        [Fact]
        public void IsValidRegion()
        {
            Assert.True(_sut.IsValidRegion(_sampleRegion));
        }

        [Fact]
        public void IsValidRegion_Null()
        {
            Assert.False(_sut.IsValidRegion(null));
            Assert.False(_sut.IsValidRegion(new ToolkitRegion()
            {
                Id = null
            }));
        }

        [Fact]
        public void IsValidRegion_LocalRegion()
        {
            var localRegion = new ToolkitRegion() { Id = "local" };
            PublishContextFixture.SetupRegionAsLocal(localRegion.Id);

            Assert.False(_sut.IsValidRegion(localRegion));
        }

        [Fact]
        public void GetValidRegion_NoCredentialProperties()
        {
            PublishContextFixture.DefineCredentialProperties(SampleCredentialIdentifier, null);

            Assert.Equal(SampleFallbackRegion, _sut.GetValidRegion(SampleCredentialIdentifier));
        }

        [Fact]
        public void GetValidRegion_WithNoRegionProperty()
        {
            var properties = new ProfileProperties();
            PublishContextFixture.DefineCredentialProperties(SampleCredentialIdentifier, properties);

            Assert.Equal(SampleFallbackRegion, _sut.GetValidRegion(SampleCredentialIdentifier));
        }

        [Fact]
        public void GetValidRegion_WithRegionProperty()
        {
            var properties = new ProfileProperties() { Region = _sampleRegion.Id, };
            PublishContextFixture.DefineCredentialProperties(SampleCredentialIdentifier, properties);

            Assert.Equal(_sampleRegion, _sut.GetValidRegion(SampleCredentialIdentifier));
        }

        [Fact]
        public void GetValidRegion_WithSsoRegionProperty()
        {
            var properties = new ProfileProperties() { SsoRegion = _sampleRegion.Id, };
            PublishContextFixture.DefineCredentialProperties(SampleCredentialIdentifier, properties);

            Assert.Equal(_sampleRegion, _sut.GetValidRegion(SampleCredentialIdentifier));
        }

        [StaFact]
        public void CreateLoadingScope()
        {
            Assert.False(_sut.IsLoading);
            var loadingScope = _sut.CreateLoadingScope();
            Assert.True(_sut.IsLoading);

            loadingScope.Dispose();
            Assert.False(_sut.IsLoading);
        }

        private void CreateSampleSummaryConfigurationDetail()
        {
            _sut.ConfigurationDetails.Add(
                ConfigurationDetailBuilder.Create()
                    .WithSampleData()
                    .IsVisible()
                    .IsSummaryDisplayable()
                    .WithName("use vpc")
                    .WithType(DetailType.Blob)
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
                    .WithType(DetailType.Blob)
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
