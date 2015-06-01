using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using System.Threading;

namespace Amazon.AWSToolkit.Tests.CloudFormationDeployment
{
    /// <summary>
    /// Summary description for StackTests
    /// </summary>
    [TestClass]
    public class StackTests
    {
        private const string
            TMPL_SINGLE_INSTANCE = "Amazon.AWSToolkit.Tests.CloudFormationDeployment.SingleInstance.template",
            TMPL_LOAD_BALANCED   = "Amazon.AWSToolkit.Tests.CloudFormationDeployment.LoadBalanced.template";

        public StackTests()
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
        public void ValidateTemplates()
        {   
            Assert.IsTrue(ValidateTemplate(TMPL_SINGLE_INSTANCE, "InstanceType", "KeyPair", "SecurityGroup", "BucketName", "ConfigFile", "AmazonMachineImage", "UserData"));
            Assert.IsTrue(ValidateTemplate(TMPL_LOAD_BALANCED));
        }

        [TestMethod]
        public void TestSingleInstanceStack()
        {
            string stackName = "ToolkitDeploymentStack" + DateTime.Now.Ticks.ToString();
            string template = LoadTemplateFromResource(TMPL_SINGLE_INSTANCE);

            Clients.CloudFormationClient.CreateStack(new CreateStackRequest()
            {
                StackName = stackName,
                TemplateBody = template,
                Parameters = new List<Parameter>()
                {
                    new Parameter(){ParameterKey = "InstanceType", ParameterValue = "t1.micro"},
                    new Parameter(){ParameterKey = "KeyPair", ParameterValue = "jimfl"},
                    new Parameter(){ParameterKey = "SecurityGroup", ParameterValue = "default"},
                    new Parameter(){ParameterKey = "BucketName", ParameterValue = "Woof"},
                    new Parameter(){ParameterKey = "UserData", ParameterValue = "TWFyayBpdCB6ZXJvISEh"},
                    new Parameter(){ParameterKey = "ConfigFile", ParameterValue = "foo"}
                }
            });


            Thread.Sleep(5000);

            var response = Clients.CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = stackName });

            Assert.IsTrue(response.Stacks.Count > 0);

            Stack testStack = response.Stacks[0];

            Console.WriteLine("Parameters:");

            foreach (var param in testStack.Parameters)
            {
                Console.WriteLine("  {0} : {1}", param.ParameterKey, param.ParameterValue);
            }

            Console.WriteLine("Outputs:");

            foreach (var output in testStack.Outputs)
            {
                Console.WriteLine("  {0} : {1}", output.OutputKey, output.OutputValue);
            }

            Clients.CloudFormationClient.DeleteStack(new DeleteStackRequest() { StackName = stackName });
        }

        private bool ValidateTemplate(string resourceName, params string[] expectedParameterNames)
        {
            var response = Clients.CloudFormationClient.ValidateTemplate(new ValidateTemplateRequest() { TemplateBody = LoadTemplateFromResource(resourceName) });

            List<string> paramNames = new List<string>();
            foreach(var param in response.Parameters)
            {
                paramNames.Add(param.ParameterKey);
                Console.WriteLine(param.ParameterKey);
            }

            foreach (var expected in expectedParameterNames)
            {
                if (!paramNames.Contains(expected))
                    return false;
            }

            return true;
        }

        public static string LoadTemplateFromResource(string resourceName)
        {
            Stream rStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            StreamReader sr = new StreamReader(rStream);
            string template = sr.ReadToEnd();
            sr.Close();
            rStream.Close();

            return template;
        }
    }
}
