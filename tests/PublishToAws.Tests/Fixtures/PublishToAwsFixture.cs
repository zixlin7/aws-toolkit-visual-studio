using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Common;

using AWS.Deploy.ServerMode.Client;

using Moq;

namespace Amazon.AWSToolkit.Tests.Publishing.Fixtures
{
    public class PublishToAwsFixture
    {
        public static readonly string SampleSessionId = Guid.NewGuid().ToString();
        public static readonly string SampleApplicationName = $"Sample-app-{Guid.NewGuid()}";

        public PublishContextFixture PublishContextFixture { get; } = new PublishContextFixture();
        public DeployToolControllerFixture DeployToolControllerFixture { get; } = new DeployToolControllerFixture();

        public PublishContext PublishContext => PublishContextFixture.PublishContext;
        public PublishApplicationContext PublishApplicationContext;
        public Mock<IDeployToolController> DeployToolController  => DeployToolControllerFixture.DeployToolController;

        public PublishToAwsFixture()
        {
            PublishApplicationContext = new PublishApplicationContext(PublishContext);

            SetupDeployToolController();
        }

        private void SetupDeployToolController()
        {
            DeployToolControllerFixture.SessionId = SampleSessionId;
            DeployToolControllerFixture.StartSessionAsyncResponse = CreateSampleSessionDetails();
            DeployToolControllerFixture.GetRecommendationsAsyncResponse = SamplePublishData.CreateSampleRecommendations();
            DeployToolControllerFixture.GetRepublishTargetsAsyncResponse = SamplePublishData.CreateSampleRepublishTargets();
            DeployToolControllerFixture.GetConfigSettingsAsyncResponse = CreateSampleConfigurationDetails(1);
            DeployToolControllerFixture.GetDeploymentDetailsAsyncResponse = CreateSampleDeploymentDetails();
        }

        public SessionDetails CreateSampleSessionDetails()
        {
            return new SessionDetails()
            {
                SessionId = SampleSessionId,
                DefaultApplicationName = SampleApplicationName,
            };
        }

        public static List<ConfigurationDetail> CreateSampleConfigurationDetails(int amount,
            Action<ConfigurationDetail> detailAdjustor = null)
        {
            var configurationDetails = new List<ConfigurationDetail>();
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
                    Type = DetailType.String
                };

                detailAdjustor?.Invoke(detail);

                configurationDetails.Add(detail);
            }

            return configurationDetails;
        }

        public GetDeploymentDetailsOutput CreateSampleDeploymentDetails()
        {
            var resources = new List<DisplayedResourceSummary>()
            {
                new DisplayedResourceSummary()
                {
                    Id = "test-dev",
                    Type = "AWS::ElasticBeanstalk::Environment",
                    Description = "Application Endpoint",
                    Data = new Dictionary<string, string>() {{"Endpoint", "http://test-dev-endpoint.com/"}}
                },
                new DisplayedResourceSummary()
                {
                    Id = "some-ecr-repo",
                    Type = "Elastic Container Registry Repository",
                }
            };

            return new GetDeploymentDetailsOutput() { CloudApplicationName = "sampleStack", DisplayedResources = resources };
        }
    }
}
