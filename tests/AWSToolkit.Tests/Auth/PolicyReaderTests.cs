using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;

namespace Amazon.AWSToolkit.Tests.Auth
{
    /// <summary>
    /// Summary description for PolicyReaderTests
    /// </summary>
    [TestClass]
    public class PolicyReaderTests
    {
        public PolicyReaderTests()
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
        public void ReadPolicyForSQSAllowingSNSMessages()
        {
            string policyStr = 
            "{" +
	        "    \"Version\": \"2008-10-17\", " +
	        "    \"Statement\": " +
	        "    [" +
		    "        {" +
		    "        \"Resource\": \"arn:aws:sqs:us-east-1:963068290131:TestSNSNotification\", " +
		    "        \"Effect\": \"Allow\", " +
		    "        \"Sid\": \"ad279892-1597-46f8-922c-eb2b545a14a8\", " +
		    "        \"Action\": \"SQS:SendMessage\", " +
		    "        \"Condition\": " +
			"            {" +
			"            \"StringLike\": " +
		    "               {" +
	        "	            \"aws:SourceArn\": \"arn:aws:sns:us-east-1:963068290131:TestSQSTopic\"" +
			"	            }" +
			"            }, " +
		    "        \"Principal\": " +
			"            {" +
			"            \"AWS\": \"*\"" +
			"            }" +
		    "        }" +
	        "    ]" +
            "}";

            Policy policy = Policy.FromJson(policyStr);
            Assert.AreEqual("2008-10-17", policy.Version);
            Assert.AreEqual(1, policy.Statements.Count);

            Statement statement = policy.Statements[0];
            Assert.AreEqual(1, statement.Resources.Count);
            Assert.AreEqual("arn:aws:sqs:us-east-1:963068290131:TestSNSNotification", statement.Resources[0].Id);
            Assert.AreEqual(Statement.StatementEffect.Allow, statement.Effect);
            Assert.AreEqual("ad279892-1597-46f8-922c-eb2b545a14a8", statement.Id);
            Assert.AreEqual(1, statement.Actions.Count);
            Assert.AreEqual(SQSActionIdentifiers.SendMessage.ActionName.ToLower(), statement.Actions[0].ActionName.ToLower());
            Assert.AreEqual(1, statement.Conditions.Count);

            Condition cond = statement.Conditions[0];
            Assert.AreEqual("StringLike", cond.Type);
            Assert.AreEqual("aws:SourceArn", cond.ConditionKey);
            Assert.AreEqual(1, cond.Values.Length);
            Assert.AreEqual("arn:aws:sns:us-eas-1:963068290131:TestSQSTopic", cond.Values[0]);

            Assert.AreEqual(1, statement.Principals.Count);
            Assert.AreEqual("AWS", statement.Principals[0].Provider);
            Assert.AreEqual("*", statement.Principals[0].Id);
        }

        [TestMethod]
        public void ArrayAndSingleAndNoAction()
        {
            string policyStr =
            "{" +
            "    \"Version\": \"2008-10-17\", " +
            "    \"Statement\": " +
            "    [" +
            "        {" +
            "        \"Resource\": \"arn:aws:sqs:us-east-1:963068290131:TestSNSNotification\", " +
            "        \"Effect\": \"Allow\", " +
            "        \"Sid\": \"ad279892-1597-46f8-922c-eb2b545a14a8\", " +
            "        \"Action\": [\"sqs:SendMessage\", \"sns:*\"] " +
            "    ]" +
            "}";

            Policy policy;
            policy = Policy.FromJson(policyStr);

            Assert.AreEqual(2, policy.Statements[0].Actions.Count);
            Assert.AreEqual("sqs:SendMessage", policy.Statements[0].Actions[0].ActionName);
            Assert.AreEqual("sns:*", policy.Statements[0].Actions[1].ActionName);


            policyStr =
                "{" +
                "    \"Version\": \"2008-10-17\", " +
                "    \"Statement\": " +
                "    [" +
                "        {" +
                "        \"Resource\": \"arn:aws:sqs:us-east-1:963068290131:TestSNSNotification\", " +
                "        \"Effect\": \"Allow\", " +
                "        \"Sid\": \"ad279892-1597-46f8-922c-eb2b545a14a8\", " +
                "        \"Action\": \"sqs:SendMessage\"" +
                "    ]" +
                "}";

            policy = Policy.FromJson(policyStr);

            Assert.AreEqual(1, policy.Statements[0].Actions.Count);
            Assert.AreEqual("sqs:SendMessage", policy.Statements[0].Actions[0].ActionName);

            policyStr =
                "{" +
                "    \"Version\": \"2008-10-17\", " +
                "    \"Statement\": " +
                "    [" +
                "        {" +
                "        \"Resource\": \"arn:aws:sqs:us-east-1:963068290131:TestSNSNotification\", " +
                "        \"Effect\": \"Allow\", " +
                "        \"Sid\": \"ad279892-1597-46f8-922c-eb2b545a14a8\", " +
                "        \"Action\": null" +
                "    ]" +
                "}";

            policy = Policy.FromJson(policyStr);

            Assert.AreEqual(0, policy.Statements[0].Actions.Count);

            policyStr =
                "{" +
                "    \"Version\": \"2008-10-17\", " +
                "    \"Statement\": " +
                "    [" +
                "        {" +
                "        \"Resource\": \"arn:aws:sqs:us-east-1:963068290131:TestSNSNotification\", " +
                "        \"Effect\": \"Allow\", " +
                "        \"Sid\": \"ad279892-1597-46f8-922c-eb2b545a14a8\" " +
                "    ]" +
                "}";

            policy = Policy.FromJson(policyStr);

            Assert.AreEqual(0, policy.Statements[0].Actions.Count);
        }
    }
}
