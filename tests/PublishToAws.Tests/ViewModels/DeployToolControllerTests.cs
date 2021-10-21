using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class DeployToolControllerTests
    {
        private readonly Mock<IRestAPIClient> _restClient = new Mock<IRestAPIClient>();
        private readonly IDeployToolController _deployToolController;
        private readonly string _sessionId = "sessionId1";
        private readonly string _applicationName = "application-name";
        private readonly string _stackName = "stackName1";
        private readonly string _recipeId = "recipeId1";
        private readonly string _projectPath = "projectPath1";
        private readonly CancellationToken _cancelToken = new CancellationToken();
        private readonly List<OptionSettingItemSummary> _optionSettingItemSummaries;

        public DeployToolControllerTests()
        {
            _deployToolController = new DeployToolController(_restClient.Object);
            _optionSettingItemSummaries = new List<OptionSettingItemSummary>()
            {
                CreateVisibleCoreOptionItemSummary
                (
                    id : "id1",
                    name : "name1",
                    description :"description1",
                    type : "String",
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
                            value: "value21"
                        ),
                        CreateVisibleCoreOptionItemSummary
                        (
                            id: "id2.2",
                            name: "name22",
                            description: "child two",
                            type: "String",
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetDeploymentTarget(bool isRepublish)
        {
            _restClient.Setup(mock =>
                    mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<SetDeploymentTargetInput>(), It.IsAny<CancellationToken>()));

            await _deployToolController.SetDeploymentTarget(_sessionId, _stackName, _recipeId, isRepublish, _cancelToken);

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny< SetDeploymentTargetInput>(), _cancelToken), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetDeploymentTarget_InvalidSessionId(bool isRepublish)
        {
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.SetDeploymentTarget("", _stackName, _recipeId, isRepublish,  _cancelToken);
            });

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken), Times.Never);
        }

        [Theory]
        [InlineData("", "", true)]
        [InlineData("", "recipeId1", true)]
        [InlineData(null, "recipeId1", true)]
        [InlineData("", "recipeId1", false)]
        [InlineData("stackName1", "", false)]
        public async Task SetDeploymentTarget_InvalidParameter(string stackName, string recipeId, bool isRepublish)
        {
            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
            {
                await _deployToolController.SetDeploymentTarget(_sessionId, stackName, recipeId, isRepublish, _cancelToken);
            });

            _restClient.Verify(mock => mock.SetDeploymentTargetAsync(_sessionId, It.IsAny<SetDeploymentTargetInput>(), _cancelToken), Times.Never);
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

            var response = await _deployToolController.GetDeploymentDetails(_sessionId, _cancelToken);

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

            var response = await _deployToolController.GetConfigSettings(_sessionId, _cancelToken);

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
                await _deployToolController.GetConfigSettings("", _cancelToken);
            });

            _restClient.Verify(mock => mock.GetConfigSettingsAsync(_sessionId, _cancelToken), Times.Never);
        }

        [Fact]
        public async Task GetConfigSettings_EmptyReturn()
        {
            var getOptionSettingsOutput = new GetOptionSettingsOutput();

            _restClient.Setup(mock =>
                    mock.GetConfigSettingsAsync(_sessionId, _cancelToken)).ReturnsAsync(getOptionSettingsOutput);

            var response = await _deployToolController.GetConfigSettings(_sessionId, _cancelToken);

            _restClient.Verify(mock => mock.GetConfigSettingsAsync(_sessionId, _cancelToken), Times.Once);

            Assert.Empty(response);
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetails()
        {
            // arrange.
            var configurationDetails = new List<ConfigurationDetail>
            {
                _optionSettingItemSummaries[0].ToConfigurationDetail(),
                _optionSettingItemSummaries[1].ToConfigurationDetail()
            };

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
            var response = await _deployToolController.ApplyConfigSettings(_sessionId, configurationDetails, _cancelToken);

            // assert.
            Assert.Equal(expectedConfiguration, actualInput.UpdatedSettings);
            Assert.Null(response.FailedConfigUpdates);
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetail()
        {
            var configurationDetail = _optionSettingItemSummaries[0].ToConfigurationDetail();
            SetupApplyConfigSettings(new ApplyConfigSettingsOutput());

            var response = await _deployToolController.ApplyConfigSettings(_sessionId, configurationDetail, _cancelToken);

            AssertApplyConfigSettingsCalled(1);
            Assert.Null(response.FailedConfigUpdates);
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetail_InvalidSessionId()
        {
            var configurationDetail = _optionSettingItemSummaries[0].ToConfigurationDetail();
            await Assert.ThrowsAsync<InvalidSessionIdException>(async () =>
            {
                await _deployToolController.ApplyConfigSettings("", configurationDetail, _cancelToken);
            });

            AssertApplyConfigSettingsCalled(0);
        }

        [Fact]
        public async Task ApplyConfigSettingsForConfigurationDetail_FailedUpdate()
        {
            var configurationDetail = _optionSettingItemSummaries[0].ToConfigurationDetail();
            var setOptionSettingsOutput = new ApplyConfigSettingsOutput {
                FailedConfigUpdates = new Dictionary<string, string>()
                {
                    { configurationDetail.Id, "unexpected error" }
                }
            };

            SetupApplyConfigSettings(setOptionSettingsOutput);

            var response = await _deployToolController.ApplyConfigSettings(_sessionId, configurationDetail, _cancelToken);

            AssertApplyConfigSettingsCalled(1);
            Assert.NotNull(response.FailedConfigUpdates);
            Assert.Single(response.FailedConfigUpdates);
            Assert.True(response.FailedConfigUpdates.ContainsKey(configurationDetail.Id));
            Assert.Equal("unexpected error", response.FailedConfigUpdates[configurationDetail.Id]);
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

        private void SetupApplyConfigSettings(ApplyConfigSettingsOutput settingsOutput)
        {
            _restClient.Setup(mock =>
                    mock.ApplyConfigSettingsAsync(_sessionId, It.IsAny<ApplyConfigSettingsInput>(), _cancelToken))
                .ReturnsAsync(settingsOutput);
        }

        private OptionSettingItemSummary CreateVisibleCoreOptionItemSummary(string id, string name, string description,
            string type, string value, List<OptionSettingItemSummary> children = null)
        {
            return new OptionSettingItemSummary()
            {
                Id = id,
                Name = name,
                Description = description,
                Type = type,
                Value = value,
                ChildOptionSettings = children,
                Visible = true,
                Advanced = false,
            };
        }
    }
}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
