using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.ViewModels;

using AWS.Deploy.ServerMode.Client;

using Moq;

namespace Amazon.AWSToolkit.Tests.Publishing.Fixtures
{
    public class DeployToolControllerFixture
    {
        public Mock<IDeployToolController> DeployToolController { get; } = new Mock<IDeployToolController>();

        public string SessionId;

        public SessionDetails StartSessionAsyncResponse;
        public List<PublishRecommendation> GetRecommendationsAsyncResponse;
        public List<RepublishTarget> GetRepublishTargetsAsyncResponse;
        public List<ConfigurationDetail> GetConfigSettingsAsyncResponse;
        public GetDeploymentDetailsOutput GetDeploymentDetailsAsyncResponse;
        public List<TargetSystemCapability> GetCompatibilityAsyncResponse = new List<TargetSystemCapability>();
        public ValidationResult ApplyConfigSettingsAsyncResponse = new ValidationResult();
        public Dictionary<string, string>
            UpdateConfigSettingValuesAsyncValueMappings = new Dictionary<string, string>();

        public DeployToolControllerFixture()
        {
            SetupDeployToolController();
        }

        private void SetupDeployToolController()
        {
            StubStartSession();
            StubGetRecommendations();
            StubGetRepublishTargets();
            StubGetConfigSettingsAsync();
            StubGetDeploymentDetailsAsync();
            StubGetCompatibilityAsync();
            StubApplyConfigSettingsAsync();
            StubUpdateConfigSettingValuesAsyncAsync();


            DeployToolController.Setup(mock =>
                mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<PublishRecommendation>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()));

            DeployToolController.Setup(mock =>
                mock.SetDeploymentTargetAsync(It.IsAny<string>(), It.IsAny<RepublishTarget>(),
                    It.IsAny<CancellationToken>()));
        }

        public void StubStartDeploymentAsyncThrowsProblemDetails()
        {
            var problemDetails = new ProblemDetails()
            {
                Detail =
                    "Unable to start deployment due to missing system capabilities.\r\nThe selected deployment option requires Docker, which was not detected. Please install and start the appropriate version of Docker for your OS. https://docs.docker.com/engine/install/\r\n"
            };

            DeployToolController.Setup(mock =>
                    mock.StartDeploymentAsync(It.IsAny<string>()))
                .ThrowsAsync(new ApiException<ProblemDetails>("", 424, "", null, problemDetails, null));
        }


        public void StubStartDeploymentAsyncThrows(string errorMessage)
        {
            DeployToolController.Setup(mock =>
                    mock.StartDeploymentAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception(errorMessage));
        }

        public void StubStartSessionAsyncThrows()
        {
            DeployToolController.Setup(mock => mock.StartSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));
        }

        public void StubStopSessionAsyncThrows()
        {
            DeployToolController.Setup(mock => mock.StopSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));
        }

        public void StubGetRepublishTargetsAsyncThrows()
        {
            DeployToolController.Setup(mock =>
                    mock.GetRepublishTargetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));
        }

        public void StubGetRecommendationsAsyncThrows()
        {
            DeployToolController.Setup(mock =>
                    mock.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));
        }

        public void StubGetConfigSettingsAsyncThrows()
        {
            DeployToolController.Setup(mock =>
                    mock.GetConfigSettingsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new DeployToolException("simulated service error"));
        }

        public void StubGetDeploymentDetailsAsyncThrows()
        {
            DeployToolController.Setup(mock =>
                        mock.GetDeploymentDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("simulated service error"));
        }

        public void StubGetCompatibilityAsyncThrows()
        {
            DeployToolController.Setup(mock =>
                            mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Throws(new DeployToolException("simulated service error"));
        }

        public void StubApplyConfigSettingsAsyncThrows()
        {
            DeployToolController.Setup(mock => mock.ApplyConfigSettingsAsync(It.IsAny<string>(),
                It.IsAny<IList<ConfigurationDetail>>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            DeployToolController.Setup(mock =>
                    mock.ApplyConfigSettingsAsync(It.IsAny<string>(), It.IsAny<ConfigurationDetail>(),
                        It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulated service error"));
        }

        public void AssertStartDeploymentCalledTimes(int times)
        {
            DeployToolController.Verify(mock => mock.StartDeploymentAsync(SessionId), Times.Exactly(times));
        }

        public void AssertGetDeploymentCalledTimes(int times)
        {
            DeployToolController.Verify(mock => mock.GetDeploymentStatusAsync(SessionId), Times.Exactly(times));
        }

        public void AssertGetConfigSettingsAsyncCalledTimes(int times)
        {
            DeployToolController.Verify(mock => mock.GetConfigSettingsAsync(SessionId, It.IsAny<CancellationToken>()), Times.Exactly(times));
        }

        private void StubStartSession()
        {
            DeployToolController.Setup(mock => mock.StartSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => StartSessionAsyncResponse);
        }

        private void StubGetRecommendations()
        {
            DeployToolController.Setup(mock => mock.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GetRecommendationsAsyncResponse);
        }

        private void StubGetRepublishTargets()
        {
            DeployToolController.Setup(mock => mock.GetRepublishTargetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GetRepublishTargetsAsyncResponse);
        }

        private void StubGetConfigSettingsAsync()
        {
            DeployToolController
                .Setup(mock => mock.GetConfigSettingsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GetConfigSettingsAsyncResponse);
        }

        private void StubGetDeploymentDetailsAsync()
        {
            DeployToolController
                .Setup(mock =>
                mock.GetDeploymentDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => GetDeploymentDetailsAsyncResponse);
        }

        private void StubGetCompatibilityAsync()
        {
            DeployToolController.Setup(mock =>
                mock.GetCompatibilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                () => GetCompatibilityAsyncResponse);
        }

        private void StubApplyConfigSettingsAsync()
        {
            DeployToolController.Setup(mock =>
                mock.ApplyConfigSettingsAsync(It.IsAny<string>(), It.IsAny<ConfigurationDetail>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(
                () => ApplyConfigSettingsAsyncResponse);

            DeployToolController.Setup(mock => mock.ApplyConfigSettingsAsync(It.IsAny<string>(),
                It.IsAny<IList<ConfigurationDetail>>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                () => ApplyConfigSettingsAsyncResponse);
        }

        private void StubUpdateConfigSettingValuesAsyncAsync()
        {
            DeployToolController.Setup(mock => mock.UpdateConfigSettingValuesAsync(It.IsAny<string>(),
                    It.IsAny<IEnumerable<ConfigurationDetail>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    (string sessionId, IEnumerable<ConfigurationDetail> configurationDetails,
                        CancellationToken cancellationToken) =>
                    {
                        var details = configurationDetails.ToList();
                        foreach (var configurationDetail in details)
                        {
                            configurationDetail.ValueMappings = UpdateConfigSettingValuesAsyncValueMappings;
                        }

                        return details;
                    });
        }

        public void SetupGetDeploymentStatusAsync(params GetDeploymentStatusOutput[] getDeploymentStatusAsyncResponses)
        {
            if (getDeploymentStatusAsyncResponses.Length == 1)
            {
                DeployToolController.Setup(mock => mock.GetDeploymentStatusAsync(SessionId))
                    .ReturnsAsync(getDeploymentStatusAsyncResponses.First());
            }
            else
            {
                var sequence = DeployToolController.SetupSequence(mock => mock.GetDeploymentStatusAsync(SessionId));

                foreach (var getDeploymentStatusAsyncResponse in getDeploymentStatusAsyncResponses)
                {
                    sequence = sequence.ReturnsAsync(getDeploymentStatusAsyncResponse);
                }
            }
        }
    }
}
