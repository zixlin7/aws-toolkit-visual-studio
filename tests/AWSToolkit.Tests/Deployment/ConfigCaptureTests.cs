using System;
using System.Collections.Generic;
using System.IO;

using AWSDeployment;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.AWSToolkit.Tests.Deployment
{
    /// <summary>
    /// Summary description for ConfigReaderTests
    /// </summary>
    [TestClass]
    public class ConfigCaptureTests
    {
        [TestMethod]
        public void TestCloudFormationCapture()
        {
            var settings = CreateBaseSettings();
            settings[CloudFormationDeploymentEngine.STACK_NAME] = "WebApplication2";

            var config = DeploymentEngineBase.CaptureEnvironmentConfig(settings);
            using (StringWriter sw = new StringWriter())
            {
                var writer = new DeploymentConfigurationWriter
                {
                    OutputEmptyItems = true,
                    Writer = sw
                };
                writer.Write(config);

                Console.WriteLine("----------------------------");
                Console.WriteLine(sw.ToString());
                Console.WriteLine("----------------------------");
            }
            Console.WriteLine(config.GetType().FullName);
        }
        [TestMethod]
        public void TestBeanstalkCapture()
        {
            var settings = CreateBaseSettings();
            settings[BeanstalkDeploymentEngine.APPLICATION_NAME] = "WebApplication2";
            settings[BeanstalkDeploymentEngine.ENVIRONMENT_NAME] = "WebApp49EB";

            var config = DeploymentEngineBase.CaptureEnvironmentConfig(settings);
            using (StringWriter sw = new StringWriter())
            {
                var writer = new DeploymentConfigurationWriter
                {
                    OutputEmptyItems = true,
                    Writer = sw
                };
                writer.Write(config);

                Console.WriteLine("----------------------------");
                Console.WriteLine(sw.ToString());
                Console.WriteLine("----------------------------");
            }
            Console.WriteLine(config.GetType().FullName);
        }

        private Dictionary<string, object> CreateBaseSettings()
        {
            return new Dictionary<string, object>
            {
                {
                    DeploymentEngineBase.ACCESS_KEY,
                    Clients.ACCESS_KEY_ID
                },
                {
                    DeploymentEngineBase.SECRET_KEY,
                    Clients.SECRET_KEY_ID
                },
                {
                    DeploymentEngineBase.REGION,
                    RegionEndPointsManager.US_EAST_1
                }
            };
        }
    }
}
