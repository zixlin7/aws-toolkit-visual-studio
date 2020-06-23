using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.ElasticBeanstalk.Commands;
using Amazon.ElasticBeanstalk.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;

using CliConstants = Amazon.ElasticBeanstalk.Tools.EBConstants;
using Amazon.AWSToolkit.Telemetry;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class WizardToEbToolsCommandTests
    {
        RegionEndPointsManager.RegionEndPoints endPoints = new RegionEndPointsManager.RegionEndPoints("us-east-1", "US East", new Dictionary<string, RegionEndPointsManager.EndPoint>(), new string[0]);

        [Fact]
        public void CompareSingleInstanceDeploy()
        {
            var properties = LoadProperties("single-instance-deploy-settings.json");

            var cliCommand = new DeployEnvironmentCommand(new TestLogger(), string.Empty, new string[0]);
            var toolkitCommand = new DeployWithEbToolsCommand(string.Empty, properties);
            toolkitCommand.SetPropertiesForDeployCommand(new BeanstalkDeploy(), cliCommand, endPoints);

            Assert.Equal(CliConstants.ENVIRONMENT_TYPE_SINGLEINSTANCE, cliCommand.DeployEnvironmentOptions.EnvironmentType);
            Assert.True(cliCommand.DeployEnvironmentOptions.EnableXRay);
            Assert.Equal(CliConstants.ENHANCED_HEALTH_TYPE_ENHANCED, cliCommand.DeployEnvironmentOptions.EnhancedHealthType);
            Assert.Equal("aws-elasticbeanstalk-ec2-role", cliCommand.DeployEnvironmentOptions.InstanceProfile);
            Assert.Equal("aws-elasticbeanstalk-service-role", cliCommand.DeployEnvironmentOptions.ServiceRole);
            Assert.Equal("netcoreapp2.1", cliCommand.DeployEnvironmentOptions.TargetFramework);
            Assert.Equal("Debug", cliCommand.DeployEnvironmentOptions.Configuration);

            Assert.Equal("EbLinux21Test", cliCommand.DeployEnvironmentOptions.Application);
            Assert.Equal("EbLinux21Test-prod", cliCommand.DeployEnvironmentOptions.Environment);
            Assert.Equal("eblinux21test-prod", cliCommand.DeployEnvironmentOptions.CNamePrefix);
            Assert.Equal("64bit Amazon Linux 2 v0.0.1 running DotNetCore", cliCommand.DeployEnvironmentOptions.SolutionStack);
            Assert.Equal("t3a.medium", cliCommand.DeployEnvironmentOptions.InstanceType);
            Assert.Equal("work-laptop", cliCommand.DeployEnvironmentOptions.EC2KeyPair);
            Assert.Equal(CliConstants.PROXY_SERVER_NGINX, cliCommand.DeployEnvironmentOptions.ProxyServer);
            Assert.Equal("v20200619062156", cliCommand.DeployEnvironmentOptions.VersionLabel);
            Assert.True(cliCommand.PersistConfigFile);

            Assert.Null(cliCommand.DeployEnvironmentOptions.LoadBalancerType);
            Assert.Null(cliCommand.DeployEnvironmentOptions.IISWebSite);
            Assert.Null(cliCommand.DeployEnvironmentOptions.UrlPath);
            
            Assert.Empty(cliCommand.DeployEnvironmentOptions.AdditionalOptions);
        }


        [Fact]
        public void CompareApplicationLoadbalancerDeploy()
        {
            var properties = LoadProperties("application-load-balancer-settings.json");


            var cliCommand = new DeployEnvironmentCommand(new TestLogger(), string.Empty, new string[0]);
            var toolkitCommand = new DeployWithEbToolsCommand(string.Empty, properties);
            toolkitCommand.SetPropertiesForDeployCommand(new BeanstalkDeploy(), cliCommand, endPoints);

            Assert.True(cliCommand.DeployEnvironmentOptions.EnableXRay);
            Assert.Equal(CliConstants.ENHANCED_HEALTH_TYPE_BASIC, cliCommand.DeployEnvironmentOptions.EnhancedHealthType);

            Assert.False(cliCommand.PersistConfigFile);

            Assert.Equal(CliConstants.ENVIRONMENT_TYPE_LOADBALANCED, cliCommand.DeployEnvironmentOptions.EnvironmentType);
            Assert.Equal(CliConstants.LOADBALANCER_TYPE_APPLICATION, cliCommand.DeployEnvironmentOptions.LoadBalancerType);

            Assert.Empty(cliCommand.DeployEnvironmentOptions.AdditionalOptions);
        }

        [Fact]
        public void CompareNetworkLoadbalancerDeploy()
        {
            var properties = LoadProperties("network-load-balancer-settings.json");


            var cliCommand = new DeployEnvironmentCommand(new TestLogger(), string.Empty, new string[0]);
            var toolkitCommand = new DeployWithEbToolsCommand(string.Empty, properties);
            toolkitCommand.SetPropertiesForDeployCommand(new BeanstalkDeploy(), cliCommand, endPoints);

            Assert.True(cliCommand.DeployEnvironmentOptions.EnableXRay);
            Assert.Equal(CliConstants.ENHANCED_HEALTH_TYPE_BASIC, cliCommand.DeployEnvironmentOptions.EnhancedHealthType);

            Assert.Equal(CliConstants.ENVIRONMENT_TYPE_LOADBALANCED, cliCommand.DeployEnvironmentOptions.EnvironmentType);
            Assert.Equal(CliConstants.LOADBALANCER_TYPE_NETWORK, cliCommand.DeployEnvironmentOptions.LoadBalancerType);

            Assert.Empty(cliCommand.DeployEnvironmentOptions.AdditionalOptions);
        }

        [Fact]
        public void CompareClassicLoadbalancerDeploy()
        {
            var properties = LoadProperties("classic-load-balancer-settings.json");


            var cliCommand = new DeployEnvironmentCommand(new TestLogger(), string.Empty, new string[0]);
            var toolkitCommand = new DeployWithEbToolsCommand(string.Empty, properties);
            toolkitCommand.SetPropertiesForDeployCommand(new BeanstalkDeploy(), cliCommand, endPoints);

            Assert.False(cliCommand.DeployEnvironmentOptions.EnableXRay);
            Assert.Equal(CliConstants.ENHANCED_HEALTH_TYPE_BASIC, cliCommand.DeployEnvironmentOptions.EnhancedHealthType);

            Assert.Equal(CliConstants.ENVIRONMENT_TYPE_LOADBALANCED, cliCommand.DeployEnvironmentOptions.EnvironmentType);
            Assert.Equal(CliConstants.LOADBALANCER_TYPE_CLASSIC, cliCommand.DeployEnvironmentOptions.LoadBalancerType);

            Assert.Empty(cliCommand.DeployEnvironmentOptions.AdditionalOptions);
        }

        [Fact]
        public void CompareNonDefaultVpcDeploy()
        {
            var properties = LoadProperties("nondefault-vpc-deploy-settings.json");


            var cliCommand = new DeployEnvironmentCommand(new TestLogger(), string.Empty, new string[0]);
            var toolkitCommand = new DeployWithEbToolsCommand(string.Empty, properties);
            toolkitCommand.SetPropertiesForDeployCommand(new BeanstalkDeploy(), cliCommand, endPoints);

            Assert.Equal(CliConstants.ENVIRONMENT_TYPE_LOADBALANCED, cliCommand.DeployEnvironmentOptions.EnvironmentType);
            Assert.Equal(CliConstants.LOADBALANCER_TYPE_APPLICATION, cliCommand.DeployEnvironmentOptions.LoadBalancerType);

            Assert.Equal("sg-4846eb32", cliCommand.DeployEnvironmentOptions.AdditionalOptions["aws:autoscaling:launchconfiguration,SecurityGroups"]);
            Assert.Equal("subnet-018c94621ef5188c2,subnet-6c8d4025", cliCommand.DeployEnvironmentOptions.AdditionalOptions["aws:ec2:vpc,Subnets"]);
            Assert.Equal("subnet-000ab696318b8f86d,subnet-0a976ae3505b98cd3,subnet-018c94621ef5188c2", cliCommand.DeployEnvironmentOptions.AdditionalOptions["aws:ec2:vpc,ELBSubnets"]);
            Assert.Equal("public", cliCommand.DeployEnvironmentOptions.AdditionalOptions["aws:ec2:vpc,ELBScheme"]);
        }

        [Fact]
        public void CompareSelfContainedRedeployNoProxy()
        {
            var properties = LoadProperties("self-contained-redeploy-no-proxy.json");

            var cliCommand = new DeployEnvironmentCommand(new TestLogger(), string.Empty, new string[0]);
            var toolkitCommand = new DeployWithEbToolsCommand(string.Empty, properties);
            toolkitCommand.SetPropertiesForDeployCommand(new BeanstalkDeploy(), cliCommand, endPoints);

            Assert.True(cliCommand.DeployEnvironmentOptions.SelfContained);
            Assert.Equal(Amazon.ElasticBeanstalk.Tools.EBConstants.PROXY_SERVER_NONE, cliCommand.DeployEnvironmentOptions.ProxyServer);

            Assert.Equal("v20200621064205", cliCommand.DeployEnvironmentOptions.VersionLabel);
            Assert.Equal("netcoreapp3.1", cliCommand.DeployEnvironmentOptions.TargetFramework);
            Assert.Equal("Debug", cliCommand.DeployEnvironmentOptions.Configuration);

            Assert.True(cliCommand.DeployEnvironmentOptions.EnableXRay);
            Assert.Equal(CliConstants.ENHANCED_HEALTH_TYPE_ENHANCED, cliCommand.DeployEnvironmentOptions.EnhancedHealthType);

        }

        private Dictionary<string, object> LoadProperties(string filename)
        {
            var content = File.ReadAllText("./ElasticBeanstalk/Resources/" + filename);
            var properties = ThirdParty.Json.LitJson.JsonMapper.ToObject<Dictionary<string, object>>(content);

            return properties;
        }

        public class TestLogger : IToolLogger
        {
            public StringBuilder Buffer { get; } = new StringBuilder();

            public void WriteLine(string message)
            {
                Buffer.AppendLine(message);
            }

            public void WriteLine(string message, params object[] args)
            {
                WriteLine(string.Format(message, args));
            }
        }
    }
}
