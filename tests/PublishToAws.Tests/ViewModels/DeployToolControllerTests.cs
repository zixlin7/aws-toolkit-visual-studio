using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class DeployToolControllerTests
    {
        private readonly Mock<IRestAPIClient> _restClient = new Mock<IRestAPIClient>();
        private readonly Mock<IPublishToAwsProperties> _publishProperties = new Mock<IPublishToAwsProperties>();
        private readonly Mock<IDialogFactory> _dialogFactory = new Mock<IDialogFactory>();
        private readonly IDeployToolController _deployToolController;
        private readonly string _sessionId = "sessionId1";
        private readonly string _applicationName = "application-name";
        private readonly string _stackName = "stackName1";
        private readonly string _recipeId = "recipeId1";
        private readonly PublishRecommendation _newPublishTarget;
        private readonly RepublishTarget _republishTarget = new RepublishTarget(new ExistingDeploymentSummary()
        {
            ExistingDeploymentId = "some-deployment",
        });
        private readonly string _projectPath = "projectPath1";
        private readonly CancellationToken _cancelToken = new CancellationToken();
        private readonly string _sampleConfigId = "sampleConfigId";
        private readonly List<OptionSettingItemSummary> _optionSettingItemSummaries;
        private readonly GetConfigSettingResourcesOutput _sampleResourcesOutput;
        private readonly ConfigurationDetailFactory _configurationDetailFactory;

        public DeployToolControllerTests()
        {
            _newPublishTarget = new PublishRecommendation(new RecommendationSummary() { RecipeId = _recipeId });

            _configurationDetailFactory = new ConfigurationDetailFactory(_publishProperties.Object, _dialogFactory.Object);
            _deployToolController = new DeployToolController(_restClient.Object, _configurationDetailFactory);
            _sampleResourcesOutput = new GetConfigSettingResourcesOutput()
            {
                Resources = new List<TypeHintResourceSummary>()
                {
                    new TypeHintResourceSummary() { SystemName = "abc", DisplayName = "def" }
                }
            };
            _optionSettingItemSummaries = new List<OptionSettingItemSummary>()
            {
                CreateVisibleCoreOptionItemSummary
                (
                    id : "id1",
                    name : "name1",
                    description :"description1",
                    type : "String",
                    typeHint:"dummyTypeHint",
                    value : "value1"
                ),
                CreateVisibleCoreOptionItemSummary
                (
                    id : "id2",
                    name : "name2",
                    description : "sample with two children",
                    type : "Object",
                    value : "value2",
                    children: new List<OptionSettingItemSummary>()
                    {
                        CreateVisibleCoreOptionItemSummary
                        (
                            id: "id2.1",
                            name: "name21",
                            description: "child one",
                            type: "String",
                            typeHint:"dummyTypeHint",
                            value: "value21"
                        ),
                        CreateVisibleCoreOptionItemSummary
                        (
                            id: "id2.2",
                            name: "name22",
                            description: "child two",
                            type: "String",
                            typeHint:"dummyTypeHint",
                            value: "value22"
                        )
                    }
                ),
                CreateVisibleCoreOptionItemSummary
                (
                    id : "id3",
                    name: "name3",
                    description: "sample with nested children",
                    type: "Object",
                    value: "value3",
                    children: new List<OptionSettingItemSummary>()
                    {
                        CreateVisibleCoreOptionItemSummary
                        (
                            id: "id3.1",
                            name: "name31",
                            description: "child one",
                            type: "Object",
                            value: "value31",
                            children: new List<OptionSettingItemSummary>()
                            {
                                CreateVisibleCoreOptionItemSummary
                                (
                                    id: "id3.1.a",
                                    name: "name31a",
                                    description: "grandchild one",
                                    type: "String",
                                    value: "value31a"
                                ),
                            }
                        ),
                    }
                )
            };
            SetupFakeGetRecipeAsync();
        }

        private void SetupFakeGetRecipeAsync()
        {
            Func<string, string, RecipeSummary> recipeSummaryFunc = (recipeId, projectPath) => SamplePublishData.GetSampleRecipeSummary(recipeId);
            _restClient.Setup(mock => mock.GetRecipeAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(recipeSummaryFunc);
        }

        [Fact]
        public async Task StartSession()
        {
            _restClient.Setup(mock => mock.StartDeploymentSessionAsync(It.IsAny<StartDeploymentSessionInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StartDeploymentSessionOutput(){SessionId =_sessionId, DefaultDeploymentName = _applicationName});

            var response = await StartSessionAsync();

            Assert.Equal(_sessionId, response.SessionId);
            Assert.Equal(_applicationName, response.DefaultApplicationName);
        }

        private async Task<SessionDetails> StartSessionAsync()
        {
            return await _deployToolController.StartSessionAsync("region1", _projectPath, _cancelToken);
        }

        public static IEnumerable<object[]> StartSessionErrorTestCases = new List<object[]>
        {
            new object[] {null},
            new object[] {new StartDeploymentSessionOutput() { SessionId = null }},
            new object[] {new StartDeploymentSessionOutput() { SessionId = "123", DefaultDeploymentName = null }}
        };

        [Theory]
        [MemberData(nameof(StartSessionErrorTestCases))]
        public async Task StartSessionShouldThrow(StartDeploymentSessionOutput output)
        {
            _restClient.Setup(mock => mock.StartDeploymentSessionAsync(It.IsAny<StartDeploymentSessionInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(output);

            await Assert.ThrowsAsync<DeployToolException>(async() => await StartSessionAsync());
        }

        [Fact]
        public async Task SetDeploymentTarget_NewPublish()
        {
            _restClient.Setup(mock =>
                mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<SetDeploymentTargetInput>(),
                    It.IsAny<CancellationToken>()));

            await _deployToolController.SetDeploymentTargetAsync(_sessionId, _newPublishTarget, _stackName, _cancelToken);

            _restClient.Verify(
                mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken),
                Times.Once);
        }

        [Fact]
        public async Task SetDeploymentTarget_Republish()
        {
            _restClient.Setup(mock =>
                mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<SetDeploymentTargetInput>(),
                    It.IsAny<CancellationToken>()));

            await _deployToolController.SetDeploymentTargetAsync(_sessionId, _republishTarget, _cancelToken);

            _restClient.Verify(
                mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken),
                Times.Once);
        }

        [Fact]
        public async Task SetDeploymentTarget_InvalidSessionId_NewPublish()
        {
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync("", _newPublishTarget, _stackName, _cancelToken);
            });

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken), Times.Never);
        }

        [Fact]
        public async Task SetDeploymentTarget_InvalidSessionId_RePublish()
        {
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync("", _republishTarget, _cancelToken);
            });

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken), Times.Never);
        }

        [Fact]
        public async Task SetDeploymentTarget_NewPublish_InvalidParameter()
        {
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(null, _newPublishTarget, _stackName, _cancelToken);
            });

            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, _newPublishTarget, null, _cancelToken);
            });

            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, null, _stackName, _cancelToken);
            });

            var publishTarget = new PublishRecommendation(new RecommendationSummary() { RecipeId = null });
            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, publishTarget, _stackName, _cancelToken);
            });

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken), Times.Never);
        }

        [Fact]
        public async Task SetDeploymentTarget_RePublish_InvalidParameter()
        {
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(null, _republishTarget, _cancelToken);
            });

            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, null, _cancelToken);
            });

            var publishTarget = new RepublishTarget(new ExistingDeploymentSummary(){ExistingDeploymentId = null});
            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
            {
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, publishTarget, _cancelToken);
            });

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken), Times.Never);
        }

        [Fact]
        public async Task SetDeploymentTargetAsync_ThrowsException()
        {
            var apiException = new ApiException("message", 400, "response", null, null);

            _restClient.Setup(mock =>
                mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<SetDeploymentTargetInput>(),
                    It.IsAny<CancellationToken>())).ThrowsAsync(apiException);

            var exception = await Assert.ThrowsAsync<ApiException>(async () =>
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, _newPublishTarget, _stackName,
                    _cancelToken));

            Assert.Equal(apiException, exception);
        }

        [Fact]
        public async Task SetDeploymentTargetAsync_ThrowsInvalidApplicationNameException()
        {
            ProblemDetails details = new ProblemDetails()
            {
                Status = 400,
                Detail = InvalidApplicationNameException.ErrorText,
            };

            var apiException = new ApiException<ProblemDetails>("qwerty", 400,
                $"{{\"detail\":\"{InvalidApplicationNameException.ErrorText}\"}}",
                null, details, null);

            _restClient.Setup(mock =>
                mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<SetDeploymentTargetInput>(),
                    It.IsAny<CancellationToken>())).ThrowsAsync(apiException);

            var exception = await Assert.ThrowsAsync<InvalidApplicationNameException>(async () =>
                await _deployToolController.SetDeploymentTargetAsync(_sessionId, _newPublishTarget, _stackName,
                    _cancelToken));

            Assert.Equal(InvalidApplicationNameException.ErrorText, exception.Message);
        }

        public static IEnumerable<object[]> GetRecommendationOutputData = new List<object[]>
        {
            new object[] {null},
            new object[] {new GetRecommendationsOutput{ Recommendations = null}},
            new object[] {new GetRecommendationsOutput()
            {
                Recommendations = new List<RecommendationSummary>()
            }}
        };

        [Fact]
        public async Task GetDeploymentDetails()
        {
            _restClient.Setup(mock => mock.GetDeploymentDetailsAsync(_sessionId, _cancelToken))
                .ReturnsAsync(new GetDeploymentDetailsOutput());

            var response = await _deployToolController.GetDeploymentDetailsAsync(_sessionId, _cancelToken);

            _restClient.Verify(mock => mock.GetDeploymentDetailsAsync(_sessionId, _cancelToken), Times.Once);
            Assert.NotNull(response);
        }

        [Theory]
        [MemberData(nameof(GetRecommendationOutputData))]
        public async Task GetRecommendations_EmptyList(GetRecommendationsOutput output)
        {
            // arrange.
            _restClient.Setup(mock => mock.GetRecommendationsAsync(_sessionId, _cancelToken))
                .ReturnsAsync(output);

            // act.
            var recommendations = await _deployToolController.GetRecommendationsAsync(_sessionId, _projectPath, _cancelToken);

            // assert.
            Assert.Empty(recommendations);
        }

        [Fact]
        public async Task GetRecommendations()
        {
            // arrange.
            var recommendationSummaries = SamplePublishData.CreateSampleRecommendationSummaries();

            _restClient.Setup(mock => mock.GetRecommendationsAsync(_sessionId, _cancelToken))
                .ReturnsAsync(new GetRecommendationsOutput() { Recommendations = recommendationSummaries});

            var expectedRecommendations = SamplePublishData.CreateSampleRecommendations();

            // act.
            var recommendations = await _deployToolController.GetRecommendationsAsync(_sessionId, _projectPath, _cancelToken);

            // assert.
            Assert.Equal(expectedRecommendations, recommendations);
        }

        public static IEnumerable<object[]> GetExistingDeploymentsOutputData = new List<object[]>
        {
            new object[] {null},
            new object[] {new GetExistingDeploymentsOutput(){ ExistingDeployments = null}},
            new object[] {new GetExistingDeploymentsOutput()
            {
                ExistingDeployments = new List<ExistingDeploymentSummary>()
            }}
        };

        [Theory]
        [MemberData(nameof(GetExistingDeploymentsOutputData))]
        public async Task GetRepublishTargets_EmptyList(GetExistingDeploymentsOutput output)
        {
            // arrange.
            _restClient.Setup(mock => mock.GetExistingDeploymentsAsync(_sessionId, _cancelToken)).ReturnsAsync(output);

            // act.
            var republishTargets = await _deployToolController.GetRepublishTargetsAsync(_sessionId, _projectPath, _cancelToken);

            // assert.
            Assert.Empty(republishTargets);
        }

        [Fact]
        public async Task GetRepublishTargets()
        {
            // arrange.
            var existingTargets = SamplePublishData.CreateSampleExistingDeployments();

            _restClient.Setup(mock => mock.GetExistingDeploymentsAsync(_sessionId, _cancelToken))
                .ReturnsAsync(new GetExistingDeploymentsOutput() { ExistingDeployments = existingTargets });

            var expectedRepublishTargets = SamplePublishData.CreateSampleRepublishTargets();

            // act.
            var republishTargets = await _deployToolController.GetRepublishTargetsAsync(_sessionId, _projectPath, _cancelToken);

            // assert.
            Assert.Equal(expectedRepublishTargets, republishTargets);
        }

        [Fact]
        public async Task GetConfigSettings()
        {
            var getOptionSettingsOutput = new GetOptionSettingsOutput {
                OptionSettings = _optionSettingItemSummaries
            };

            _restClient.Setup(mock =>
                    mock.GetConfigSettingsAsync(_sessionId, _cancelToken)).ReturnsAsync(getOptionSettingsOutput);

            var response = await _deployToolController.GetConfigSettingsAsync(_sessionId, _cancelToken);

            _restClient.Verify(mock => mock.GetConfigSettingsAsync(_sessionId, _cancelToken), Times.Once);

            Assert.Equal(3, response.Count);

            var detailWithTwoChildren = response[1];
            var detailWithNestedChildren = response[2];

            Assert.Equal(_optionSettingItemSummaries[0].Id, response[0].Id);
            Assert.Equal(_optionSettingItemSummaries[1].Id, detailWithTwoChildren.Id);
            Assert.Equal(_optionSettingItemSummaries[2].Id, detailWithNestedChildren.Id);

            // Option with two children
            Assert.Equal(2, detailWithTwoChildren.Children.Count);
            Assert.Contains(detailWithTwoChildren.Children, detail => detail.Id == "id2.1");
            Assert.Contains(detailWithTwoChildren.Children, detail => detail.Id == "id2.2");

            // Option with nested children
            var nestedChild = Assert.Single(detailWithNestedChildren.Children);
            Assert.Equal("id3.1", nestedChild.Id);
            var nestedGrandChild = Assert.Single(nestedChild.Children);
            Assert.Equal("id3.1.a", nestedGrandChild.Id);
            Assert.Empty(nestedGrandChild.Children);
        }

        [Fact]
        public async Task GetConfigSettings_InvalidSessionId()
        {
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.GetConfigSettingsAsync("", _cancelToken);
            });

            _restClient.Verify(mock => mock.GetConfigSettingsAsync(_sessionId, _cancelToken), Times.Never);
        }

        [Fact]
        public async Task GetConfigSettings_EmptyReturn()
        {
            var getOptionSettingsOutput = new GetOptionSettingsOutput();

            _restClient.Setup(mock =>
                    mock.GetConfigSettingsAsync(_sessionId, _cancelToken)).ReturnsAsync(getOptionSettingsOutput);

            var response = await _deployToolController.GetConfigSettingsAsync(_sessionId, _cancelToken);

            _restClient.Verify(mock => mock.GetConfigSettingsAsync(_sessionId, _cancelToken), Times.Once);

            Assert.Empty(response);
        }

        public static IEnumerable<object[]> EmptyGetConfigSettingResourcesOutput = new List<object[]>
        {
            new object[] { new GetConfigSettingResourcesOutput() },
            new object[] { new GetConfigSettingResourcesOutput() {Resources = null} }
        };

        [Theory]
        [MemberData(nameof(EmptyGetConfigSettingResourcesOutput))]
        public async Task GetConfigSettingValues_EmptyReturn(GetConfigSettingResourcesOutput output)
        {
            StubGetConfigSettingValues(_sampleConfigId, output);

            var configResources =
                await _deployToolController.GetConfigSettingValuesAsync(_sessionId, _sampleConfigId, _cancelToken);

            Assert.False(configResources.Any());
            VerifyGetConfigSettingValuesCall( _sampleConfigId);
        }

        [Fact]
        public async Task GetConfigSettingValues_NotFoundExceptionIsHandled()
        {
            StubGetConfigSettingValuesWithException(404);

            var configResources =
                await _deployToolController.GetConfigSettingValuesAsync(_sessionId, _sampleConfigId, _cancelToken);

            Assert.False(configResources.Any());
            VerifyGetConfigSettingValuesCall(_sampleConfigId);
        }

        [Fact]
        public async Task GetConfigSettingValues_Exception()
        {
            StubGetConfigSettingValuesWithException(500);

            await Assert.ThrowsAsync<ApiException>(async () =>
            {
                await _deployToolController.GetConfigSettingValuesAsync(_sessionId, _sampleConfigId,
                        _cancelToken);
            });

            VerifyGetConfigSettingValuesCall(_sampleConfigId);
        }


        [Fact]
        public async Task GetConfigSettingValues()
        {
            StubGetConfigSettingValues(_sampleConfigId, _sampleResourcesOutput);
           
            var configResources =
                await _deployToolController.GetConfigSettingValuesAsync(_sessionId, _sampleConfigId, _cancelToken);

            Assert.True(configResources.ContainsKey("abc"));
            VerifyGetConfigSettingValuesCall( _sampleConfigId);
        }

        [Fact]
        public async Task RetrieveConfigSettingResources_LeafConfigurationDetail()
        {
            var configurationDetails = SetupResourcesForConfigDetail(_configurationDetailFactory.CreateFrom(_optionSettingItemSummaries[0]));

            Assert.False(configurationDetails.First().HasValueMappings());

            var configResources =
                await _deployToolController.UpdateConfigSettingValuesAsync(_sessionId, configurationDetails, _cancelToken);

            AssertValueMappingContainsKey(configResources.First());
            VerifyGetConfigSettingValuesCall(_optionSettingItemSummaries[0].Id);
        }

        [Fact]
        public async Task RetrieveConfigSettingResources_ParentConfigurationDetail()
        {
            var configurationDetails = SetupResourcesForConfigDetail(_configurationDetailFactory.CreateFrom(_optionSettingItemSummaries[1]));

            var configResources =
                await _deployToolController.UpdateConfigSettingValuesAsync(_sessionId, configurationDetails, _cancelToken);

            configResources.First().Children.ToList()
                .ForEach(x =>
                {
                    AssertValueMappingContainsKey(x);
                    VerifyGetConfigSettingValuesCall(x.GetLeafId());
                });
        }

        public static IEnumerable<object[]> TypeHintUnsupportedConfigs = new List<object[]>
        {
            new object[] { ConfigurationDetailBuilder.Create().Build() },
            new object[] { ConfigurationDetailBuilder.Create().WithType(DetailType.String).Build() },
            new object[] { ConfigurationDetailBuilder.Create().WithType(DetailType.List).Build() },
            new object[] { ConfigurationDetailBuilder.Create().WithType(DetailType.Integer).WithTypeHint("dummy").Build() }
        };

        [Theory]
        [MemberData(nameof(TypeHintUnsupportedConfigs))]
        public async Task RetrieveConfigSettingResources_NoTypeHint(ConfigurationDetail detail)
        {
            var configurationDetails = SetupResourcesForConfigDetail(detail);

            var configResources =
                await _deployToolController.UpdateConfigSettingValuesAsync(_sessionId, configurationDetails, _cancelToken);

            var updatedSetting = configResources.First();
            Assert.False(updatedSetting.ValueMappings.ContainsKey("abc"));
            _restClient.Verify(
                mock => mock.GetConfigSettingResourcesAsync(_sessionId, updatedSetting.GetLeafId(), _cancelToken),
                Times.Never);

        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetails()
        {
            // arrange.
            var configurationDetails = _optionSettingItemSummaries.Take(2)
                .Select(_configurationDetailFactory.CreateFrom)
                .ToList();

            ApplyConfigSettingsInput actualInput = null;

            Func<string, ApplyConfigSettingsInput, CancellationToken, ApplyConfigSettingsOutput> applyConfigSettingsFunc = (sessionId, input, token) => {
                actualInput = input;
                return new ApplyConfigSettingsOutput();
            };

            _restClient.Setup(mock => mock.ApplyConfigSettingsAsync(It.IsAny<string>(), It.IsAny<ApplyConfigSettingsInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(applyConfigSettingsFunc);

            var expectedConfiguration = new Dictionary<string, string>()
            {
                {"id1", "value1"},
                {"id2.id2.1", "value21"},
                {"id2.id2.2", "value22"}
            };

            // act.
            var validation = await _deployToolController.ApplyConfigSettingsAsync(_sessionId, configurationDetails, _cancelToken);

            // assert.
            Assert.Equal(expectedConfiguration, actualInput.UpdatedSettings);
            Assert.False(validation.HasErrors());
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetails_WithReadOnlyDetail()
        {
            // arrange.
            var configurationDetails = _optionSettingItemSummaries.Take(2)
                .Select(_configurationDetailFactory.CreateFrom)
                .ToList();

            var readOnlyDetail = configurationDetails[0];
            readOnlyDetail.ReadOnly = true;
            var writableDetail = configurationDetails[1];

            ApplyConfigSettingsInput actualInput = null;

            Func<string, ApplyConfigSettingsInput, CancellationToken, ApplyConfigSettingsOutput> applyConfigSettingsFunc = (sessionId, input, token) => {
                actualInput = input;
                return new ApplyConfigSettingsOutput();
            };

            _restClient.Setup(mock => mock.ApplyConfigSettingsAsync(It.IsAny<string>(), It.IsAny<ApplyConfigSettingsInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(applyConfigSettingsFunc);

            // act.
            await _deployToolController.ApplyConfigSettingsAsync(_sessionId, configurationDetails, _cancelToken);

            // assert.
            Assert.False(actualInput.UpdatedSettings.ContainsKey(readOnlyDetail.GetLeafId()));
            var expectedIdsTransmitted = writableDetail.GetSelfAndDescendants()
                .Where(x => x.IsLeaf())
                .Select(x => x.GetLeafId())
                .ToList();
            Assert.Equal(expectedIdsTransmitted, actualInput.UpdatedSettings.Keys);
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetail()
        {
            var configurationDetail = _configurationDetailFactory.CreateFrom(_optionSettingItemSummaries[0]);
            SetupApplyConfigSettings(new ApplyConfigSettingsOutput());

            var validation = await _deployToolController.ApplyConfigSettingsAsync(_sessionId, configurationDetail, _cancelToken);

            AssertApplyConfigSettingsCalled(1);
            Assert.False(validation.HasErrors());
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetail_InvalidSessionId()
        {
            var configurationDetail = _configurationDetailFactory.CreateFrom(_optionSettingItemSummaries[0]);
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.ApplyConfigSettingsAsync("", configurationDetail, _cancelToken);
            });

            AssertApplyConfigSettingsCalled(0);
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetail_FailedUpdate()
        {
            var configurationDetail = _configurationDetailFactory.CreateFrom(_optionSettingItemSummaries[0]);
            var setOptionSettingsOutput = new ApplyConfigSettingsOutput {
                FailedConfigUpdates = new Dictionary<string, string>()
                {
                    { configurationDetail.Id, "unexpected error" }
                }
            };

            SetupApplyConfigSettings(setOptionSettingsOutput);

            var validation = await _deployToolController.ApplyConfigSettingsAsync(_sessionId, configurationDetail, _cancelToken);

            AssertApplyConfigSettingsCalled(1);
            Assert.True(validation.HasErrors());
            Assert.Single(validation.GetErrantDetailIds());
            Assert.True(validation.HasError(configurationDetail.Id));
            Assert.Equal("unexpected error", validation.GetError(configurationDetail.Id));
        }

        [Fact]
        public async Task GetCompatibilityAsync()
        {
            var compatibilitySummary = new SystemCapabilitySummary()
            {
                Name = "compatibility-1",
                Message = "System compatibility not found."
            };
            var compatibilities = new GetCompatibilityOutput();
            compatibilities.Capabilities = new List<SystemCapabilitySummary>() {
                compatibilitySummary
            };

            _restClient.Setup(mock => mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(compatibilities);

            var response = await _deployToolController.GetCompatibilityAsync(_sessionId, _cancelToken);

            var expectedCapabilities = new List<TargetSystemCapability> { new TargetSystemCapability(compatibilitySummary) };

            Assert.Equal(expectedCapabilities, response);
        }

        private void AssertApplyConfigSettingsCalled(int times)
        {
            _restClient.Verify(
                mock => mock.ApplyConfigSettingsAsync(_sessionId, It.IsAny<ApplyConfigSettingsInput>(), _cancelToken),
                Times.Exactly(times));
        }

        private static void AssertValueMappingContainsKey(ConfigurationDetail detail)
        {
            Assert.True(detail.ValueMappings.ContainsKey("abc"));
        }

        private List<ConfigurationDetail> SetupResourcesForConfigDetail(ConfigurationDetail configDetail)
        {
            var configurationDetails = new List<ConfigurationDetail> { configDetail };
            if (configDetail.IsLeaf())
            {
                StubGetConfigSettingValues(configDetail.GetLeafId(), _sampleResourcesOutput);
            }
            else
            {
                configurationDetails.First().Children.ToList()
                    .ForEach(x => StubGetConfigSettingValues(x.GetLeafId(), _sampleResourcesOutput));
            }

            return configurationDetails;
        }

        private void VerifyGetConfigSettingValuesCall(string configId)
        {
            _restClient.Verify(mock => mock.GetConfigSettingResourcesAsync(_sessionId, configId, _cancelToken),
                Times.Once);
        }

        private void StubGetConfigSettingValuesWithException(int errorCode)
        {
            _restClient.Setup(mock => mock.GetConfigSettingResourcesAsync(_sessionId, _sampleConfigId, _cancelToken))
                .ThrowsAsync(new ApiException("error", errorCode, "error message", null, null));
        }

        private void StubGetConfigSettingValues(string configId, GetConfigSettingResourcesOutput output)
        {
            _restClient.Setup(mock => mock.GetConfigSettingResourcesAsync(_sessionId, configId, _cancelToken))
                .ReturnsAsync(output);
        }


        private void SetupApplyConfigSettings(ApplyConfigSettingsOutput settingsOutput)
        {
            _restClient.Setup(mock =>
                    mock.ApplyConfigSettingsAsync(_sessionId, It.IsAny<ApplyConfigSettingsInput>(), _cancelToken))
                .ReturnsAsync(settingsOutput);
        }

        private OptionSettingItemSummary CreateVisibleCoreOptionItemSummary(string id, string name, string description,
            string type, string value, string typeHint = null, List<OptionSettingItemSummary> children = null)
        {
            return new OptionSettingItemSummary()
            {
                Id = id,
                Name = name,
                Description = description,
                Type = type,
                Value = value,
                TypeHint = typeHint,
                ChildOptionSettings = children,
                Visible = true,
                Advanced = false,
            };
        }
    }
}
