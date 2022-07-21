using System.Collections.Generic;
using System.IO;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.VisualStudio.DeploymentProcessors;

using Newtonsoft.Json;

using Xunit;

namespace AWSToolkitPackage.Tests.DeploymentProcessors
{
    public class BeanstalkConfigurationTests
    {

        [Fact]
        public void ShouldCreateNewConfiguration()
        {
            // arrange.
            string emptyFilePath = "";

            var expectedConfiguration = new Dictionary<string, object>
            {
                { "comment", "This file is used to help set default values when using the dotnet CLI extension Amazon.ElasticBeanstalk.Tools. For more information run \"dotnet eb --help\" from the project root."}
            };

            // act.
            var configuration = BeanstalkConfiguration.CreateOrGetFrom(emptyFilePath);

            // assert.
            AssertConfigurationsAreEqual(expectedConfiguration, configuration);
        }

        private void AssertConfigurationsAreEqual(IDictionary<string, object> expectedConfiguration, BeanstalkConfiguration configuration)
        {
            Assert.Equal(expectedConfiguration, ConvertToDictionary(configuration.ToJson()));
        }

        private readonly string simpleConfigurationFilePath = "DeploymentProcessors/simple-beanstalk-configuration.json";

        [Fact]
        public void ShouldLoadExistingConfiguration()
        {
            // arrange.
            var expectedConfiguration = new Dictionary<string, object>
            {
                ["application"] = "SampleASPApp",
                ["environment"] = "SampleASPApp-dev"
            };

            // act.
            var configuration = BeanstalkConfiguration.CreateOrGetFrom(simpleConfigurationFilePath);

            // assert.
            AssertConfigurationsAreEqual(expectedConfiguration, configuration);
        }

        [Fact]
        public void ShouldPrettyPrintExistingConfiguration()
        {
            // arrange.
            var expectedString = "{\r\n  \"application\": \"SampleASPApp\",\r\n  \"environment\": \"SampleASPApp-dev\"\r\n}";

            // act.
            var configuration = BeanstalkConfiguration.CreateOrGetFrom(simpleConfigurationFilePath);

            // assert.
            Assert.Equal(expectedString, configuration.ToJson());
        }

        [Fact]
        public void ShouldUpdateConfiguration()
        {
            // arrange.
            var deploymentTaskInfo = CreateDeploymentTaskInfoWith(CreateSampleOptions());

            IDictionary<string, object> expectedConfiguration = GetExpectedConfiguration();

            // act.
            BeanstalkConfiguration configuration = BeanstalkConfiguration.CreateDefault();
            configuration.UpdateConfigurationWith(deploymentTaskInfo);

            // assert.
            AssertConfigurationsAreEqual(expectedConfiguration, configuration);
        }

        private IDictionary<string, object> CreateSampleOptions()
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
            return new ToolkitRegion() { PartitionId = "aws", Id = "us-west-2", DisplayName = "us west two" };
        }

        private DeploymentTaskInfo CreateDeploymentTaskInfoWith(IDictionary<string, object> options)
        {
            return new DeploymentTaskInfo(null, null, null, null, options, null, null);
        }

        private IDictionary<string, object> GetExpectedConfiguration()
        {
            string configurationJson = File.ReadAllText("DeploymentProcessors/expected-aws-beanstalk-configuration.json");
            return ConvertToDictionary(configurationJson);
        }

        private IDictionary<string, object> ConvertToDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        [Fact]
        public void ShouldUpdateConfigurationWithoutXRay()
        {
            // arrange.
            var options = CreateSampleOptions();
            options["enableXRayDaemon"] = false;

            var deploymentTaskInfo = CreateDeploymentTaskInfoWith(options);

            IDictionary<string, object> expectedConfiguration = GetExpectedConfiguration();
            expectedConfiguration["enable-xray"] = false;

            // act.
            BeanstalkConfiguration configuration = BeanstalkConfiguration.CreateDefault();
            configuration.UpdateConfigurationWith(deploymentTaskInfo);

            // assert.
            AssertConfigurationsAreEqual(expectedConfiguration, configuration);
        }

        [Fact]
        public void ShouldUpdateConfigurationWithComplicatedAppPath()
        {
            // arrange.
            var options = CreateSampleOptions();
            options["deployIisAppPath"] = "dist/my-app-path";

            var deploymentTaskInfo = CreateDeploymentTaskInfoWith(options);

            IDictionary<string, object> expectedConfiguration = GetExpectedConfiguration();
            expectedConfiguration["iis-website"] = "dist";
            expectedConfiguration["app-path"] = "/my-app-path";

            // act.
            BeanstalkConfiguration configuration = BeanstalkConfiguration.CreateDefault();
            configuration.UpdateConfigurationWith(deploymentTaskInfo);

            // assert.
            AssertConfigurationsAreEqual(expectedConfiguration, configuration);
        }
    }
}
