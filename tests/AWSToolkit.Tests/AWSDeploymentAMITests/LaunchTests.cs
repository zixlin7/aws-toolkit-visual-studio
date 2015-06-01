using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeployment;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.Common.TestCredentials;

namespace Amazon.AWSToolkit.Tests.AWSDeploymentAMITests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class LaunchTests
    {
        static TestCredentials defaultCredentials = TestCredentials.DefaultCredentials;

        static string _region = "sa-east-1";
        static string _amiId = "ami-3abe6027";
        static string _keyPair;
        static string _privateKey;

        static string ACCESS_KEY = defaultCredentials.AccessKey;
        static string SECRET_KEY = defaultCredentials.SecretKey;

        static CloudFormationTemplateWrapper _template;
        static IAmazonCloudFormation _cloudFormationClient;
        static IAmazonEC2 _ec2Client;

        public LaunchTests()
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

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            var cloudConfig = new AmazonCloudFormationConfig();
            cloudConfig.ServiceURL = string.Format("https://cloudformation.{0}.amazonaws.com", _region);
            _cloudFormationClient = new AmazonCloudFormationClient(ACCESS_KEY, SECRET_KEY, cloudConfig);

            var ec2Config = new AmazonEC2Config();
            ec2Config.ServiceURL = string.Format("https://ec2.{0}.amazonaws.com", _region);
            _ec2Client = new AmazonEC2Client(ACCESS_KEY, SECRET_KEY, ec2Config);

            createKeyPair();

            _template = CloudFormationTemplateWrapper.FromPublicS3Location("http://vstoolkit.amazonwebservices.com/CloudFormationTemplates/SingleInstance.template");
            _template.LoadAndParse();
        }

        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            deleteKeyPair();
        }



        [TestMethod]
        public void VisualStudio2010MVC3()
        {
            deploy("TestVS2010MVC3.zip", true);
        }

        [TestMethod]
        public void VisualStudio2010MVC2()
        {
            deploy("VisualStudio2010MVC2.zip", true);
        }

        [TestMethod]
        public void AWS2010WebApp()
        {
            deploy("AWS2010WebApp.zip", true);
        }

        [TestMethod]
        public void NBlog()
        {
            deploy("NBlog.zip", true);
        }

        [TestMethod]
        public void DynamicCompression()
        {
            var deployment = deploy("DynamicCompression.zip", false, true);

            var url = getStackURL(deployment);

            try
            {
                bool found = false;
                for (int i = 0; i < 10; i++)
                {
                    var request = HttpWebRequest.Create(url);
                    request.Headers["Accept-Encoding"] = "gzip,deflate,sdch";

                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (response.Headers["Content-Encoding"] == "gzip")
                        {
                            found = true;
                            break;
                        }
                    }
                }

                Assert.IsTrue(found, "Failed to find compression header");
            }
            finally
            {
                deleteStack(deployment.StackName);
            }
        }

        [TestMethod]
        public void PetBoardVS2010()
        {
            deploy("PetBoardVS2010.zip", true, true);
        }

        static AWSDeployment.CloudFormationDeploymentEngine deploy(string archive, bool delete)
        {
            return deploy(archive, delete, false);
        }

        static AWSDeployment.CloudFormationDeploymentEngine deploy(string archive, bool delete, bool targetV2Runtime)
        {
            var bucketName = "aws-cloudformation-test-deployments-" + _region;
            var filepath = writeArchiveToTemp(archive);
            
            AWSDeployment.CloudFormationDeploymentEngine deployment 
                = AWSDeployment.DeploymentEngineFactory.CreateEngine(AWSDeployment.DeploymentEngineFactory.CloudFormationServiceName)
                    as AWSDeployment.CloudFormationDeploymentEngine;

            deployment.Region = _region;
            deployment.DeploymentPackage = filepath;
            deployment.KeyPairName = _keyPair;
            deployment.AWSAccessKey = ACCESS_KEY;
            deployment.AWSSecretKey = SECRET_KEY;
            deployment.EnvironmentProperties["AWSAccessKey"] = ACCESS_KEY;
            deployment.EnvironmentProperties["AWSSecretKey"] = SECRET_KEY;

            deployment.StackName = archive.Replace(".", "") + DateTime.Now.Ticks;
            deployment.Template = _template.TemplateContent;
            deployment.UploadBucket = bucketName;

            deployment.TemplateParameters["SecurityGroup"] = "default";
            if(!string.IsNullOrEmpty(_amiId))
                deployment.TemplateParameters["AmazonMachineImage"] = _amiId;

            deployment.TargetRuntime = targetV2Runtime ? "2" : "4";
            deployment.Observer = new TestObserver();
            try
            {
                deployment.Deploy();

                string url;
                var success = waitForStackCompletion(deployment, out url);
                Assert.IsTrue(success);

                validate200StatusCode(url);
            }
            finally
            {
                if(delete)
                    deleteStack(deployment.StackName);
            }

            return deployment;
        }

        static void deleteStack(string stackName)
        {
            var request = new DeleteStackRequest() { StackName = stackName };
            _cloudFormationClient.BeginDeleteStack(request, null, null);
        }

        static string writeArchiveToTemp(string archive)
        {
            var filepath = Path.GetTempFileName() + ".zip";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Tests.Archives." + archive))
            {
                var data = new Byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
                File.WriteAllBytes(filepath, data);
            }

            return filepath;
        }

        static void createKeyPair()
        {
            _keyPair = "vidro-tests-" + DateTime.Now.Ticks;
            var createResponse = _ec2Client.CreateKeyPair(new CreateKeyPairRequest() { KeyName = _keyPair });
            _privateKey = createResponse.KeyPair.KeyMaterial;
        }

        static void deleteKeyPair()
        {
            if (string.IsNullOrEmpty(_keyPair))
                return;

            _ec2Client.DeleteKeyPair(new DeleteKeyPairRequest() { KeyName = _keyPair });
        }

        static void logMessage(string message, params string[] values)
        {
            string fullMessage = string.Format(message, values);
            Console.WriteLine(DateTime.Now.ToString() + ": " + fullMessage);
        }

        static void validate200StatusCode(string url)
        {
            try
            {
                var request = HttpWebRequest.Create(url);
                using (var response = request.GetResponse() as HttpWebResponse)
                {

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new ApplicationException(string.Format("Failed to get 200 status code for URL {0}.  Got {1}", url, response.StatusCode));
                    }

                    if (searchForContent(response, "<title>IIS7</title>"))
                    {
                        throw new ApplicationException(string.Format("Got default IIS page instead of application page.", url, response.StatusCode));
                    }
                }
            }
            catch (WebException e)
            {
                logMessage(string.Format("Failed to make web request to {0} with error {1}", url, e.Message));
                throw;
            }

        }

        static bool searchForContent(HttpWebResponse response, string content)
        {
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string data = reader.ReadToEnd();
                return data.Contains(content);
            }
        }

        static string getStackURL(AWSDeployment.CloudFormationDeploymentEngine deployment)
        {
            var info = deployment.GetStackInfo();
            var query = from output in info.Outputs where output.OutputKey.Equals("URL") select output.OutputValue;
            if (query != null)
            {
                var url = query.First();
                return url;
            }

            return null;
        }

        static bool waitForStackCompletion(AWSDeployment.CloudFormationDeploymentEngine deployment, out string url)
        {
            url = null;
            TimeSpan THIRTY_SECONDS = new TimeSpan(0, 0, 30);
            bool rollback = false;

            for (/* LOOP */ ; /* FOR */ ; /* EVAR */ )
            {
                System.Threading.Thread.Sleep(THIRTY_SECONDS);
                var info = deployment.GetStackInfo();
                if (info != null)
                {
                    if (info.StackStatus.Equals("CREATE_COMPLETE"))
                    {
                        deployment.Observer.Status("Application deployment completed.");
                        url = getStackURL(deployment);
                        if (url != null)
                        {
                            deployment.Observer.Info("URL is {0}", url.First());
                        }
                        return true;
                    }
                    else if (info.StackStatus.Equals("CREATE_FAILED"))
                    {
                        deployment.Observer.Error("Application deployment failed: {0}", info.StackStatusReason);
                        return false;
                    }
                    else if (info.StackStatus.Equals("ROLLBACK_IN_PROGRESS") && !rollback)
                    {
                        deployment.Observer.Error("Stack creation being rolled back: {0}", info.StackStatusReason);
                        rollback = true;
                    }
                    else if (info.StackStatus.Equals("ROLLBACK_COMPLETE"))
                    {
                        deployment.Observer.Error("Rollback complete.");
                        return false;
                    }
                }
            }
        }

        public class TestObserver : DeploymentObserver
        {
            public override void Status(string messageFormat, params object[] list) 
            {
                log(messageFormat, list);
            }
            public override void Progress(string messageFormat, params object[] list)
            {
                log(messageFormat, list);
            }

            public override void Info(string messageFormat, params object[] list)
            {
                log(messageFormat, list);
            }

            public override void Warn(string messageFormat, params object[] list)
            {
                log(messageFormat, list);
            }

            public override void Error(string messageFormat, params object[] list)
            {
                log(messageFormat, list);
            }

            void log(string format, params object[] list)
            {
                log(string.Format(format, list));
            }

            void log(string message)
            {
                Console.WriteLine("{0} - {1}", DateTime.Now.TimeOfDay, message);
            }
        }
    }
}
