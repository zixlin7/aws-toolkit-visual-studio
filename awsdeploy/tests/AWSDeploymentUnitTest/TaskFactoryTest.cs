using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeploymentHostManager;
using ThirdParty.Json.LitJson;
using System.Security.Cryptography;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class TaskFactoryTest
    {
        [TestMethod]
        public void TestTaskCreation()
        {
            TestUtil.SetHostManagerConfig(new HostManagerConfig("{}"));

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            TaskFactory factory = new TaskFactory();

            factory.RegisterTask("TestTask", typeof(TestTask));

            JsonData parameters = new JsonData();
            JsonData request = new JsonData();

            request["name"] = "TestTask";

            parameters["one"] = "ENO";
            parameters["two"] = "OWT";
            parameters["six"] = "XIS";

            request["parameters"] = parameters;

            string rString = JsonMapper.ToJson(request);

            Task task = factory.CreateTaskFromRequest(rString);

            Assert.IsTrue(task is TestTask);
            Assert.AreEqual<string>("TestTask", task.Operation);

            string response = task.Execute();

            Console.WriteLine("JSON Response from task:");
            Console.WriteLine(response);

            JsonData jResponse = JsonMapper.ToObject(response);
            Assert.AreEqual<string>("TestTask", (string)jResponse["operation"]);
            Assert.AreEqual<string>("ok", (string)jResponse["response"]);
            Assert.AreEqual<string>("ENO", (string)jResponse["one"]);
            Assert.AreEqual<string>("OWT", (string)jResponse["two"]);
            Assert.AreEqual<string>("XIS", (string)jResponse["six"]);
        }
    }
}
