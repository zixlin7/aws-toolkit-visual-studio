using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.AWSToolkit.SQS;
using Amazon.AWSToolkit.SQS.Model;

namespace Amazon.AWSToolkit.Tests.SQS
{
    /// <summary>
    /// Summary description for PermissionsTests
    /// </summary>
    [TestClass]
    public class PermissionsTests
    {
        public PermissionsTests()
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
        public void ParsePolicyJson()
        {
            
            string policy = "{\"Version\":\"2008-10-17\",\"Id\":\"arn:aws:sqs:us-east-1:599169622985:TestQueue801107754/SQSDefaultPolicy\",\"Statement\":[{\"Sid\":\"d7e7e86e-f36e-471d-aeb5-98a3790bc1e5\",\"Effect\":\"Allow\",\"Principal\":{\"AWS\":\"599169622985\"},\"Action\":\"SQS:DeleteMessage\",\"Resource\":\"arn:aws:sqs:us-east-1:599169622985:TestQueue801107754\"},{\"Sid\":\"f8187b34-3732-481e-846c-c5f23f85e156\",\"Effect\":\"Allow\",\"Principal\":{\"AWS\":\"599169622985\"},\"Action\":\"SQS:*\",\"Resource\":\"arn:aws:sqs:us-east-1:599169622985:TestQueue801107754\"}]}";
            QueuePermissionsModel model = new QueuePermissionsModel(policy);

            Assert.AreEqual(2, model.Permissions.Count);
            Assert.IsNotNull(model.Permissions[0].Action);
            Assert.IsNotNull(model.Permissions[0].AWSAccountId);
            Assert.IsNotNull(model.Permissions[0].Label);

            Assert.IsNotNull(model.Permissions[1].Action);
            Assert.IsNotNull(model.Permissions[1].AWSAccountId);
            Assert.IsNotNull(model.Permissions[1].Label);
        }
    }
}
