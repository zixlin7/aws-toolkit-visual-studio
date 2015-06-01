using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeploymentHostManager.Tasks;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class TailTest
    {
        [TestMethod]
        public void TestTail()
        {
            TailTask task = new TailTask();

            JsonData json = JsonMapper.ToObject(task.Execute());
            Assert.AreEqual<string>(task.Operation, (string)json["operation"]);

            string data = Encoding.UTF8.GetString(Convert.FromBase64String((string)json["response"]));

            string year = DateTime.Now.ToString("yyyy");

            // This is an IIS Log file, so, a tail either starts with a '#' (if the file is shorter than 105 lines) 
            // Or with the current year (a log file entry).
            Assert.IsTrue(data[0].Equals('#') || data.StartsWith(year));

            Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String((string)json["response"])));
        }
    }
}
