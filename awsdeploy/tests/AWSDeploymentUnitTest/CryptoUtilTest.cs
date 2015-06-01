using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web;
using AWSDeploymentHostManager;
using System.Security.Cryptography;

using ThirdParty.Json.LitJson;
using AWSDeploymentCryptoUtility;

namespace AWSDeploymentUnitTest
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CryptoUtilTest
    {
        public CryptoUtilTest()
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
        public void TestRoundTripCrypto()
        {
            string target = "Never Gonna Give You Up / Never Gonna Let You Down";

            // Create an instance of AES to get a test key and IV from
            Aes testAes = Aes.Create();
            testAes.KeySize = 256;

            byte[] ciphertext = CryptoUtil.EncryptString(target, testAes.Key, testAes.IV);

            string plaintext = CryptoUtil.DecryptBytes(ciphertext, testAes.Key, testAes.IV);

            Assert.AreEqual<string>(target, plaintext, "Round trip encryption resulted in different message");
        }

        [TestMethod]
        public void TestRoundTripCryptoWithBase64Encoding()
        {
            string target = "We're no strangers to love / You know the rules and so do I";

            Aes testAes = Aes.Create();
            testAes.KeySize = 256;

            string ciphertext = CryptoUtil.EncryptToBase64EncodedString(target, testAes.Key, testAes.IV);
            string plaintext  = CryptoUtil.DecryptFromBase64EncodedString(ciphertext, testAes.Key, testAes.IV);

            Assert.AreEqual<string>(target, plaintext, "Round trip encryption resulted in different message");
        }

        [TestMethod]
        public void TestKeymatterIntegrator()
        {
            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                return String.Format("onetwothreefour{0}", ts);
            };

            Assert.AreEqual<string>("onetwothreefourfivesix", merge("fivesix"));
        }

        [TestMethod]
        public void TestDecryptRequest()
        {
            Aes testAes = Aes.Create();
            testAes.KeySize = 256;

            string payload = "Royale with cheese";

            string iv = Convert.ToBase64String(testAes.IV);
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                return String.Format("GobbleDeeGook{0}", ts);
            };

            SHA256 hash = new SHA256Managed();
            System.Text.Encoding encoding = new System.Text.UTF8Encoding();
            byte[] key = hash.ComputeHash(encoding.GetBytes(merge(timestamp)));

            string cPayload = CryptoUtil.EncryptToBase64EncodedString(payload, key, testAes.IV);

            JsonData jData = new JsonData();

            jData["iv"] = iv;
            jData["timestamp"] = timestamp;
            jData["payload"] = cPayload;

            string jString = JsonMapper.ToJson(jData);

            Console.WriteLine("Encrypted JSON String:");
            Console.WriteLine(jString);

            string result = (string)CryptoUtil.DecryptRequest(jString, merge)["payload"];


            Assert.AreEqual<string>(payload, result);
        }

        [TestMethod]
        public void TestRoundTripRequestResponse()
        {
            Aes testAes = Aes.Create();
            testAes.KeySize = 256;

            string iv = Convert.ToBase64String(testAes.IV);
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                return String.Format("Mango{0}Chutney", ts);
            };

            string payload = "This battlestation is now the ultimate power in the universe.";

            // Yes, request = EncryptResponse. This is the client-eye view.
            string request = CryptoUtil.EncryptResponse(payload, testAes.IV, timestamp, merge);

            Console.WriteLine("Encrypted JSON String:");
            Console.WriteLine(request);

            JsonData jData = CryptoUtil.DecryptRequest(request, merge);

            Assert.AreEqual<string>(payload, (string)jData["payload"]);
        }

        [TestMethod]
        public void TestDecryptRequestFromRubyClient()
        {
            string rClientRequest = @"{""iv"":""IV+D3iVZmsbS0GQoSn8aUA=="",""timestamp"":""2011-03-11T08:55:33-08:00"",""payload"":""YD1ONFvxnfQO8dL+hgyG5OMiucKCuhs/qMoc0tM9r6s=""}";

            string Ec2InstanceId = "i-8e4e00ca";
            string Ec2ReservationId = "r-f41760b0";

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                string keymatter = String.Format("{0}{1}{2}", Ec2InstanceId, Ec2ReservationId, ts);
                Console.WriteLine("Raw Keymatter: {0}", keymatter);
                SHA256 hash = new SHA256Managed();
                System.Text.Encoding encoding = new System.Text.ASCIIEncoding();
                Console.WriteLine("Base64 Encoded key: {0}", Convert.ToBase64String(hash.ComputeHash(encoding.GetBytes(keymatter))));

                return keymatter;
            };

            JsonData jResponse = CryptoUtil.DecryptRequest(rClientRequest, merge);

            string responsePayload = (string)jResponse["payload"];

            Console.WriteLine("Response payload: {0}", responsePayload);

            Assert.IsNotNull(responsePayload);

            Assert.AreEqual<string>("Status", (string)(JsonMapper.ToObject(responsePayload)["name"]));
        }

        //[TestMethod]
        public void TestRoundTripCryptoWithConfigurableAlgorithm()
        {
            int iters = 1000;

            int start = Environment.TickCount;
            for (int i = 0; i < iters; i++)
            {
                Aes testAes = Aes.Create();
                testAes.Mode = CipherMode.CBC;
                testAes.KeySize = 256;

                testAes.Clear();
                testAes = null;
            }
            int duration = Environment.TickCount - start;
            Console.WriteLine("{0} AES create operations took {1} ms.", iters, duration);

            Aes aesForKeys = Aes.Create();
            aesForKeys.KeySize = 256;

            string target = "I've got a fever and the only prescription is More Cowbell!";

            start = Environment.TickCount;
            for (int i = 0; i < iters; i++)
            {
                string ciphertext = CryptoUtil.EncryptToBase64EncodedString(target, aesForKeys.Key, aesForKeys.IV);
                string plaintext = CryptoUtil.DecryptFromBase64EncodedString(ciphertext, aesForKeys.Key, aesForKeys.IV);                   
            }
            duration = Environment.TickCount - start;

            Console.WriteLine("{0} Round trip crypto operations took {1} m.s", iters, duration);
        }

        [TestMethod]
        public void TestQMRequest()
        {
            string encoded = "%7B%22timestamp%22%3A%222011-03-17T20%3A47%3A15%22%2C%22iv%22%3A%224zdRTi%2B2ObKgUBqcafUVyA%3D%3D%22%2C%22payload%22%3A%22TL2UmUG9P9mgu1f5BtVLIeNH5bolJewUos4n0Iohemi8rfNuCjSpPFxQxUe%5C%2FQ5g5%22%7D";

            string decoded = HttpUtility.UrlDecode(encoded);

            string Ec2InstanceId = "i-8856a0e7";
            string Ec2ReservationId = "r-4240352f";

            CryptoUtil.EncryptionKeyTimestampIntegrator merge = delegate(string ts)
            {
                string keymatter = String.Format("{0}{1}{2}", Ec2InstanceId, Ec2ReservationId, ts);
                return keymatter;
            };


            string decrypted = JsonMapper.ToJson(CryptoUtil.DecryptRequest(decoded, merge));
        }
    }
}
