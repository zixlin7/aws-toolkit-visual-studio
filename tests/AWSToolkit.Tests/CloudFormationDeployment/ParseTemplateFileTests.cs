using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

namespace Amazon.AWSToolkit.Tests.CloudFormationDeployment
{
    /// <summary>
    /// Summary description for ParseTemplateFileTests
    /// </summary>
    [TestClass]
    public class ParseTemplateFileTests
    {
        private const string TEST_TEMPLATE = "Amazon.AWSToolkit.Tests.CloudFormationDeployment.sample-test.template";

        public ParseTemplateFileTests()
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
        public void CheckParameters()
        {
            string filepath = getTemplateFile(TEST_TEMPLATE);
            try
            {
                var wrapper = CloudFormationTemplateWrapper.FromLocalFile(filepath) 
                    as CloudFormationTemplateWrapper;
                wrapper.LoadAndParse();

                Assert.AreEqual(4, wrapper.Parameters.Count);

                Assert.AreEqual("InstanceType", wrapper.Parameters["InstanceType"].Name);
                Assert.AreEqual("Type of EC2 instance to launch", wrapper.Parameters["InstanceType"].Description);
                Assert.AreEqual("String", wrapper.Parameters["InstanceType"].Type);
                Assert.AreEqual("m1.small", wrapper.Parameters["InstanceType"].DefaultValue);

                Assert.AreEqual("WebServerPort", wrapper.Parameters["WebServerPort"].Name);
                Assert.AreEqual("The TCP port for the Web Server", wrapper.Parameters["WebServerPort"].Description);
                Assert.AreEqual("String", wrapper.Parameters["WebServerPort"].Type);
                Assert.AreEqual("8888", wrapper.Parameters["WebServerPort"].DefaultValue);

                Assert.AreEqual("KeyName", wrapper.Parameters["KeyName"].Name);
                Assert.AreEqual("The EC2 Key Pair to allow SSH access to the instances", wrapper.Parameters["KeyName"].Description);
                Assert.AreEqual("String", wrapper.Parameters["KeyName"].Type);
                Assert.IsNull(wrapper.Parameters["KeyName"].DefaultValue);
            }
            finally
            {
                File.Delete(filepath);
            }
        }

        public string getTemplateFile(string filepath)
        {
            string template = StackTests.LoadTemplateFromResource(filepath);

            string file = Path.GetTempFileName();
            File.WriteAllText(file, template);
            return file;
        }

    }
}
