using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;

using ThirdParty.Json.LitJson;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Tasks;

using Amazon.S3;
using Amazon.S3.Model;
using AWSDeploymentCryptoUtility;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class ConfigTest
    {   
        [TestMethod]
        public void TestConfigFromJson()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AWSDeploymentUnitTest.Resources.sample-configfile.txt");

            StreamReader sr = new StreamReader(stream);

            string json = sr.ReadToEnd().Trim();

            HostManagerConfig config = new HostManagerConfig(json);

            Assert.AreEqual<string>("UpdatedParam", config["Application/Environment Properties/PARAM1"]);
            Assert.AreEqual<string>("elasticbeanstalk-us-east-1", config["AWSDeployment/Application/s3bucket"]);
        }

        [TestMethod]
        public void TestConfigFromS3()
        {
            string bucketName = "AWSDeploymentUnitTest" + DateTime.Now.Ticks;
            string keyName    = Guid.NewGuid().ToString() + ".war";

            var s3 = new AmazonS3Client(Constants.ACCESS_KEY_ID, Constants.SECRET_KEY_ID);

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AWSDeploymentUnitTest.Resources.sample-configfile.txt");
            StreamReader sr = new StreamReader(stream);
            string json = sr.ReadToEnd().Trim();

            Aes crypto = Aes.Create();
            crypto.Mode = CipherMode.CBC;
            crypto.KeySize = 256;
            crypto.Padding = PaddingMode.PKCS7;
            
            byte[] keybytes = crypto.Key;
            byte[] ivbytes = crypto.IV;

            string key = Convert.ToBase64String(keybytes);
            string iv = Convert.ToBase64String(ivbytes);

            string ciphertext = CryptoUtil.EncryptToBase64EncodedString(json, keybytes, ivbytes);
            try
            {
                s3.PutBucket(new PutBucketRequest() { BucketName = bucketName });

                s3.PutObject(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentBody = ciphertext
                });

                string url = s3.GetPreSignedURL(new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.Now.AddDays(10)
                });

                HostManagerConfig config = HostManagerConfig.CreateFromS3(url, key, iv);

                Assert.AreEqual<string>("UpdatedParam", config["Application/Environment Properties/PARAM1"]);
                Assert.AreEqual<string>("elasticbeanstalk-us-east-1", config["AWSDeployment/Application/s3bucket"]);
            }
            finally
            {
                s3.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest() { BucketName = bucketName, Key = keyName });
                s3.DeleteBucket(new Amazon.S3.Model.DeleteBucketRequest() { BucketName = bucketName });
            }
        }

        [TestMethod]
        public void TestConfigFromUserData()
        {
            string bucketName = "AWSDeploymentUnitTest" + DateTime.Now.Ticks;
            string keyName = Guid.NewGuid().ToString() + ".war";

            var s3 = new AmazonS3Client(Constants.ACCESS_KEY_ID, Constants.SECRET_KEY_ID);

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AWSDeploymentUnitTest.Resources.sample-configfile.txt");
            StreamReader sr = new StreamReader(stream);
            string json = sr.ReadToEnd().Trim();

            string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            try
            {
                s3.PutBucket(new PutBucketRequest() { BucketName = bucketName });

                s3.PutObject(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentBody = encoded
                });

                StringBuilder sb = new StringBuilder();

                JsonWriter jw = new JsonWriter(sb);

                jw.WriteObjectStart();

                jw.WritePropertyName("credentials");
                jw.WriteObjectStart();
                jw.WritePropertyName("accessKey");
                jw.Write(Constants.ACCESS_KEY_ID);
                jw.WritePropertyName("secretKey");
                jw.Write(Constants.SECRET_KEY_ID);
                jw.WriteObjectEnd();

                jw.WritePropertyName("configuration");
                jw.WriteObjectStart();
                jw.WritePropertyName("s3Bucket");
                jw.Write(bucketName);
                jw.WritePropertyName("s3Key");
                jw.Write(keyName);
                jw.WriteObjectEnd();

                jw.WriteObjectEnd();

                string userdata = sb.ToString();

                Console.WriteLine("UserData: {0}", userdata);

                HostManagerConfig config = HostManagerConfig.CreateFromUserData(userdata);

                Assert.AreEqual<string>("UpdatedParam", config["Application/Environment Properties/PARAM1"]);
                Assert.AreEqual<string>("elasticbeanstalk-us-east-1", config["AWSDeployment/Application/s3bucket"]);
                Assert.AreEqual<string>(Constants.ACCESS_KEY_ID, config["credentials/AWS_ACCESS_KEY_ID"]);
            }
            finally
            {
                s3.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest() { BucketName = bucketName, Key = keyName });
                s3.DeleteBucket(new Amazon.S3.Model.DeleteBucketRequest() { BucketName = bucketName });
            }
        }

        [TestMethod]
        public void TestUpdateConfigTask()
        {
            string bucketName = "AWSBeanstalkUnitTest" + DateTime.Now.Ticks;
            string keyName = Guid.NewGuid().ToString() + ".war";

            var s3 = new AmazonS3Client(Constants.ACCESS_KEY_ID, Constants.SECRET_KEY_ID);

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AWSDeploymentUnitTest.Resources.sample-configfile.txt");
            StreamReader sr = new StreamReader(stream);
            string json = sr.ReadToEnd().Trim();

            Aes crypto = Aes.Create();
            crypto.Mode = CipherMode.CBC;
            crypto.KeySize = 256;
            crypto.Padding = PaddingMode.PKCS7;

            byte[] keybytes = crypto.Key;
            byte[] ivbytes = crypto.IV;

            string key = Convert.ToBase64String(keybytes);
            string iv = Convert.ToBase64String(ivbytes);

            string ciphertext = CryptoUtil.EncryptToBase64EncodedString(json, keybytes, ivbytes);

            try
            {
                s3.PutBucket(new PutBucketRequest() { BucketName = bucketName });

                s3.PutObject(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentBody = ciphertext
                });

                string url = s3.GetPreSignedURL(new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.Now.AddDays(10)
                });

                StringBuilder sb = new StringBuilder();
                JsonWriter jw = new JsonWriter(sb);

                jw.WriteObjectStart();

                    jw.WritePropertyName("name");
                    jw.Write("UpdateConfiguration");

                    jw.WritePropertyName("parameters");
                    jw.WriteObjectStart();

                        jw.WritePropertyName("configUrl");
                        jw.Write(url);

                        jw.WritePropertyName("key");
                        jw.Write(key);

                        jw.WritePropertyName("iv");
                        jw.Write(iv);

                    jw.WriteObjectEnd();
                jw.WriteObjectEnd();

                string requestJson = sb.ToString();
                TestUtil.SetHostManagerConfig(new HostManagerConfig(@"{""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}"));
                TaskFactory tf = new TaskFactory();

                tf.RegisterTask("UpdateConfiguration", typeof(UpdateConfigurationTask));

                Task updateTask = tf.CreateTaskFromRequest(requestJson);

                updateTask.Execute();

                HostManagerConfig config = ((UpdateConfigurationTask)updateTask).NewConfiguration;

                Assert.AreEqual<string>("UpdatedParam", config["Application/Environment Properties/PARAM1"]);
                Assert.AreEqual<string>("elasticbeanstalk-us-east-1", config["AWSDeployment/Application/s3bucket"]);

                ConfigVersion vers = ConfigVersion.LoadLatestVersion();

                Assert.IsNotNull(vers);

                Assert.AreEqual<string>(url, vers.Url);
                Assert.AreEqual<string>(key, vers.CryptoKey);
                Assert.AreEqual<string>(iv, vers.CryptoIV);

            }
            finally
            {
                s3.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest() { BucketName = bucketName, Key = keyName });
                s3.DeleteBucket(new Amazon.S3.Model.DeleteBucketRequest() { BucketName = bucketName });
            }


        }

    }
}
