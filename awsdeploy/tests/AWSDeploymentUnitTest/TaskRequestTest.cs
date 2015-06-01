using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.IO;
using System.IO.Pipes;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Tasks;

using ThirdParty.Json.LitJson;
using AWSDeploymentCryptoUtility;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class TaskRequestTest
    {
        [TestMethod]
        public void TestSimpleRequest()
        {
            string Ec2InstanceId = "i-8e4e00ca";
            string Ec2ReservationId = "r-f41760b0";

            string hmConfig = @"{""ec2InstanceId"" : ""i-8e4e00ca"", ""ec2ReservationId"" : ""r-f41760b0""}";

            Aes testAes = Aes.Create();
            testAes.KeySize = 256;

            string payload = @"{""name"" : ""Status""}";

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                return String.Format("{0}{1}{2}", Ec2InstanceId, Ec2ReservationId, ts);
            };

            string request = CryptoUtil.EncryptResponse(payload, testAes.IV, CryptoUtil.Timestamp(), merge);

            HostManager hm = new HostManager(hmConfig);

            string response = TestUtil.CallStringNonPublicHostManagerMethod(hm, "ProcessTaskRequest", new object[] {request});

            JsonData jResponse = CryptoUtil.DecryptRequest(response, merge);

            string responsePayload = (string)jResponse["payload"];

            Assert.IsNotNull(responsePayload);

            Console.WriteLine("Payload: {0}", responsePayload);

            Assert.AreEqual<string>("Status", (string)(JsonMapper.ToObject(responsePayload)["operation"]));
        }

        [TestMethod]
        public void TestUnknownRequest()
        {
            string Ec2InstanceId = "i-8e4e00ca";
            string Ec2ReservationId = "r-f41760b0";

            string hmConfig = @"{""ec2InstanceId"" : ""i-8e4e00ca"", ""ec2ReservationId"" : ""r-f41760b0""}";

            Aes testAes = Aes.Create();
            testAes.KeySize = 256;

            string payload = @"{""name"" : ""Frrble""}";

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                return String.Format("{0}{1}{2}", Ec2InstanceId, Ec2ReservationId, ts);
            };

            string request = CryptoUtil.EncryptResponse(payload, testAes.IV, CryptoUtil.Timestamp(), merge);

            HostManager hm = new HostManager(hmConfig);

            string response = TestUtil.CallStringNonPublicHostManagerMethod(hm, "ProcessTaskRequest", new object[] { request });

            JsonData jResponse = CryptoUtil.DecryptRequest(response, merge);

            string responsePayload = (string)jResponse["payload"];

            Assert.IsNotNull(responsePayload);

            Assert.AreEqual<string>("Frrble", (string)(JsonMapper.ToObject(responsePayload)["operation"]));
            Assert.AreEqual<string>("unknown", (string)(JsonMapper.ToObject(responsePayload)["response"]));
       }

        [TestMethod]
        public void TestRequestFromRubyClient()
        {
            string Ec2InstanceId = "i-8e4e00ca";
            string Ec2ReservationId = "r-f41760b0";
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            string hmConfig = @"{""ec2InstanceId"" : ""i-8e4e00ca"", ""ec2ReservationId"" : ""r-f41760b0""}";

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                string keymatter = String.Format("{0}{1}{2}", Ec2InstanceId, Ec2ReservationId, ts);
                Console.WriteLine("Raw Keymatter: {0}", keymatter);
                SHA256 hash = new SHA256Managed();
                System.Text.Encoding encoding = new System.Text.ASCIIEncoding();
                Console.WriteLine("Base64 Encoded key: {0}", Convert.ToBase64String(hash.ComputeHash(encoding.GetBytes(keymatter))));

                return keymatter;
            };

            string rClientRequest = CryptoUtil.EncryptResponse("{\"name\":\"Status\"}", Convert.FromBase64String("IV+D3iVZmsbS0GQoSn8aUA=="), timestamp, merge);

            HostManager hm = new HostManager(hmConfig);

            string response = TestUtil.CallStringNonPublicHostManagerMethod(hm, "ProcessTaskRequest", new object[] { rClientRequest });

            JsonData jResponse = CryptoUtil.DecryptRequest(response, merge);

            string responsePayload = (string)jResponse["payload"];

            Console.WriteLine("Response payload: {0}", responsePayload);

            Assert.IsNotNull(responsePayload);

            Assert.AreEqual<string>("Status", (string)(JsonMapper.ToObject(responsePayload)["operation"]));
        }
    }
}
