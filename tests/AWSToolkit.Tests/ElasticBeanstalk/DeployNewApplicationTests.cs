using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk;
using Amazon.AWSToolkit.ElasticBeanstalk.Commands;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

namespace Amazon.AWSToolkit.Tests.ElasticBeanstalk
{
    /// <summary>
    /// Summary description for DeployNewApplicationTests
    /// </summary>
    [TestClass]
    public class DeployNewApplicationTests
    {
        public DeployNewApplicationTests()
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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void DeployApp()
        {
            ToolkitFactory.InitializeToolkit(new NavigatorControl(), new TestShellProvider(), null);

            string appName = "TestDeployApp" + DateTime.Now.Ticks;
            string envName = "TestDeployEnv" + DateTime.Now.Ticks;

            

            var deploymentPackage = "";
            var deploymentProperties = new Dictionary<string, object>();
            deploymentProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy] = false;
            deploymentProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] = appName;
            deploymentProperties[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_AppDescription] = "App Description";
            deploymentProperties[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] = "v1.0";

            var command = new DeployNewApplicationCommand(deploymentPackage, deploymentProperties);
            //command.Execute();
        }
    }
}
