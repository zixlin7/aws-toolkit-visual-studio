using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;

using AWSDeployment;

namespace Amazon.AWSToolkit.Tests.Deployment
{
    /// <summary>
    /// Summary description for ConfigReaderTests
    /// </summary>
    [TestClass]
    public class ConfigReaderTests
    {
        static string RESOURCE_CONFIG_FILE = "Amazon.AWSToolkit.Tests.Deployment.TestDeployment.conf";
        public ConfigReaderTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void TestReadFromStream()
        {
            AWSDeployment.CloudFormationDeploymentEngine dep;

            using (var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_CONFIG_FILE))
            {
                dep = DeploymentConfigurationReader.ReadDeploymentFromStream(configStream, false) 
                    as AWSDeployment.CloudFormationDeploymentEngine;
            }

            Assert.AreEqual<string>("MyStack", dep.StackName);
            Assert.AreEqual<string>("us-west-1", dep.Region);
            Assert.AreEqual<string>("bleep", dep.KeyPairName);
            Assert.AreEqual<string>("awsdeploy-us-east-1-201009191500", dep.UploadBucket);
            Assert.AreEqual<string>(@"c:\Users\rnixon\Projects\MyWebProject.zip", dep.DeploymentPackage);

            Assert.IsFalse(dep.TargetRuntime.StartsWith("2"));
            Assert.IsTrue(dep.Enable32BitApplications.GetValueOrDefault());
            Assert.AreEqual<string>("/healthcheck", dep.ApplicationHealthcheckPath);

            Assert.AreEqual<string>("foo", dep.EnvironmentProperties["PARAM1"]);
            Assert.AreEqual<string>("bar", dep.EnvironmentProperties["PARAM2"]);
            Assert.AreEqual<string>("", dep.EnvironmentProperties["PARAM3"]);
            Assert.AreEqual<string>("", dep.EnvironmentProperties["PARAM4"]);
            Assert.AreEqual<string>("", dep.EnvironmentProperties["PARAM5"]);

            Assert.AreEqual<string>("***ACCESS-KEY***", dep.EnvironmentProperties["AWSAccessKey"]);
            Assert.AreEqual<string>("***SECRET-KEY***", dep.EnvironmentProperties["AWSSecretKey"]);

            Assert.AreEqual<string>("z1.zebra", dep.TemplateParameters["InstanceType"]);
            Assert.AreEqual<string>("foobar", dep.TemplateParameters["SecurityGroup"]);

            Assert.IsFalse(dep.Settings.RollbackOnFailure);
            Assert.AreEqual<int>(23, dep.Settings.CreationTimeout);
            Assert.AreEqual<string>("foobarbaz", dep.Settings.SNSTopic);

            Assert.IsTrue(dep.Template.Trim().StartsWith("{"));
            Assert.IsTrue(dep.Template.Trim().EndsWith("}"));

        }

        [TestMethod]
        public void TestReadWithOverrides()
        {
            AWSDeployment.CloudFormationDeploymentEngine dep;
            Dictionary<string, string> overrides = new Dictionary<string, string>();

            overrides["KeyPair"] = "frrble";
            overrides["DeploymentPackage"] = "nowhere/really";
            overrides["Environment.PARAM3"] = "lalala";
            overrides["Container.TargetRuntime"] = "2.0";
            overrides["Settings.CreationTimeout"] = "42";

            using (var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_CONFIG_FILE))
            {
                dep = DeploymentConfigurationReader.ReadDeploymentFromStream(configStream, overrides, false, null) 
                    as AWSDeployment.CloudFormationDeploymentEngine;
            }

            Assert.AreEqual<string>("frrble", dep.KeyPairName);
            Assert.AreEqual<string>("nowhere/really", dep.DeploymentPackage);
            Assert.AreEqual<string>("lalala", dep.EnvironmentProperties["PARAM3"]);
            Assert.IsTrue(dep.TargetRuntime.StartsWith("2"));
            Assert.AreEqual<int>(42, dep.Settings.CreationTimeout);
        }

        [TestMethod]
        public void TestReadFromFile()
        {
            string tmpFile = Path.GetTempFileName();
            
            using (var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_CONFIG_FILE))
            {
                using (var fs = new FileStream(tmpFile, FileMode.Open))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    StreamReader sr = new StreamReader(configStream);

                    sw.Write(sr.ReadToEnd());
                }
            }

            var dep = DeploymentConfigurationReader.ReadDeploymentFromFile(tmpFile, false) 
                as AWSDeployment.CloudFormationDeploymentEngine;
            File.Delete(tmpFile);

            Assert.AreEqual<string>("MyStack", dep.StackName);
        }
    }
}
