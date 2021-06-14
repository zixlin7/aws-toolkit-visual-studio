﻿using System.Collections.Generic;
using System.IO;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.VisualStudio.DeploymentProcessors;

using Newtonsoft.Json;

using Xunit;

namespace AWSToolkitPackage.Tests.DeploymentProcessors
{
    public class BeanstalkDeploymentProcessorTests
    {

        [Fact]
        public void ShouldGetBeanstalkConfigurationFromTaskInfo()
        {
            // arrange.
            var deploymentTaskInfo = CreateDeploymentTaskInfoWith(CreateSampleOptions());

            Dictionary<string, object> expectedConfiguration = GetExpectedConfiguration();

            // act.
            BeanstalkDeploymentProcessor processor = new BeanstalkDeploymentProcessor();
            string configurationJson = processor.GetBeanstalkConfigurationFromTaskInfo(deploymentTaskInfo);

            // assert.
            Assert.Equal(expectedConfiguration, ConvertToDictionary(configurationJson));
        }

        private Dictionary<string, object> CreateSampleOptions()
        {
            return new Dictionary<string, object>
            {
                { "selected_account", CreateSDKAccount() },
                { "selectedRegion", CreateUsWestToolkitRegion() },
                { "name", "SampleASPApp" },
                { "envName", "SampleASPApp-dev" },
                { "health-check-url", "test.example.com/ping" },
                { "solutionStack", "64bit Amazon Linux 2 v2.2.0 running .NET Core" },
                { "envType", "SingleInstance" },
                { "cName", "sampleaspapp-dev" },
                { "instanceTypeID", "t1.micro" },
                { "keyPairName", "" },
                { "instanceProfile", "aws-elasticbeanstalk-ec2-role" },
                { "serviceRole", "aws-elasticbeanstalk-service-role" },
                { "deployIisAppPath", "my-path" },
                { "enableXRayDaemon", true}
            };
        }

        private AccountViewModel CreateSDKAccount()
        {
            return new AccountViewModel(null, null, new SDKCredentialIdentifier("sdkProfile"), null);
        }

        private ToolkitRegion CreateUsWestToolkitRegion()
        {
            return new ToolkitRegion() {PartitionId = "aws", Id = "us-west-2", DisplayName = "us west two"};
        }

        private DeploymentTaskInfo CreateDeploymentTaskInfoWith(Dictionary<string, object> options)
        {
            return new DeploymentTaskInfo(null, null, null, null, options, null, null);
        }

        private Dictionary<string, object> GetExpectedConfiguration()
        {
            string configurationJson = File.ReadAllText("../../DeploymentProcessors/expected-aws-beanstalk-configuration.json");
            return ConvertToDictionary(configurationJson);
        }

        private Dictionary<string, object> ConvertToDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        [Fact]
        public void ShouldGetBeanstalkConfigurationFromTaskInfoWithoutXRay()
        {
            // arrange.
            var options = CreateSampleOptions();
            options["enableXRayDaemon"] = false;

            var deploymentTaskInfo = CreateDeploymentTaskInfoWith(options);

            Dictionary<string, object> expectedConfiguration = GetExpectedConfiguration();
            expectedConfiguration["enable-xray"] = false;

            // act.
            BeanstalkDeploymentProcessor processor = new BeanstalkDeploymentProcessor();
            string configurationJson = processor.GetBeanstalkConfigurationFromTaskInfo(deploymentTaskInfo);

            // assert.
            Assert.Equal(expectedConfiguration, ConvertToDictionary(configurationJson));
        }

        [Fact]
        public void ShouldGetBeanstalkConfigurationFromTaskInfoWithComplicatedAppPath()
        {
            // arrange.
            var options = CreateSampleOptions();
            options["deployIisAppPath"] = "dist/my-app-path";

            var deploymentTaskInfo = CreateDeploymentTaskInfoWith(options);

            Dictionary<string, object> expectedConfiguration = GetExpectedConfiguration();
            expectedConfiguration["iis-website"] = "dist";
            expectedConfiguration["app-path"] = "/my-app-path";

            // act.
            BeanstalkDeploymentProcessor processor = new BeanstalkDeploymentProcessor();
            string configurationJson = processor.GetBeanstalkConfigurationFromTaskInfo(deploymentTaskInfo);

            // assert.
            Assert.Equal(expectedConfiguration, ConvertToDictionary(configurationJson));
        }
    }
}
