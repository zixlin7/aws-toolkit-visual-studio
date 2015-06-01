using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Web.Administration;
using Microsoft.Web.Deployment;

using System.Threading;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Tasks;
using AWSDeploymentHostManager.Persistence;

using ThirdParty.Json.LitJson;

using Amazon.S3;
using Amazon.S3.Model;


namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class UpdateApplicationTest
    {
        [TestMethod]
        public void Deploy()
        {
            makeSureApplicationDoesNotExist("/ASite");

            PersistenceManager.ResetDatabaseForTesting();
            string versionId = null;
            string bucketName = "AWSBeanstalkUnitTest" + DateTime.Now.Ticks;
            string keyName = "asite.zip";
            var client = new AmazonS3Client(Constants.ACCESS_KEY_ID, Constants.SECRET_KEY_ID);

            client.PutBucket(new PutBucketRequest() { BucketName = bucketName });
            try
            {
                client.PutBucketVersioning(new PutBucketVersioningRequest()
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig() { Status = VersionStatus.Enabled }
                });
                
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AWSBeanstalkUnitTest.Resources.asite.zip"))
                {
                    var request = new Amazon.S3.Model.PutObjectRequest()
                    {
                        BucketName = bucketName,
                        Key = keyName
                    };
                    request.InputStream = stream;
                    var response = client.PutObject(request);
                    versionId = response.VersionId;
                }

                string s3Url = client.GetPreSignedURL(new Amazon.S3.Model.GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    VersionId = versionId,
                    Expires = DateTime.Now.AddMinutes(30),
                    Verb = HttpVerb.GET
                });

                DateTime start = DateTime.Now;

                Thread.Sleep(100);

                UpdateAppVersionTask task = new UpdateAppVersionTask();
                task.SetParameter("versionUrl", s3Url);
                string execResponse = task.Execute();

                JsonData json = JsonMapper.ToObject(execResponse);

                Console.WriteLine("Response: {0}", execResponse);

                Assert.AreEqual(task.Operation, (string)json["operation"]);
                Assert.AreEqual("deferred", (string)json["response"]);

                bool gotFinalEvent = false;
                for (int i = 0; i < 100 && !gotFinalEvent; i++)
                {
                    Thread.Sleep(1000);
                    IList<Event> events = Event.LoadEventsSince(start);
                    foreach (Event evt in events)
                    {
                        Console.WriteLine("Event[{0}]: {1}", evt.Severity, evt.Message);

                        string value;
                        if (evt.ExtraParameters.TryGetValue("FinalEvent", out value))
                        {
                            gotFinalEvent = true;
                            break;
                        }
                    }
                }
                
            }
            finally
            {
                client.PutBucketVersioning(new PutBucketVersioningRequest()
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig() { Status = VersionStatus.Suspended }
                });

                try
                {
                    client.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest()
                    {
                        BucketName = bucketName,
                        VersionId = versionId,
                        Key = keyName
                    });
                }
                catch { }
                client.DeleteBucket(new DeleteBucketRequest() { BucketName = bucketName });
            }
        }

        [TestMethod]
        public void ParseUrl()
        {
            var client = new AmazonS3Client(Constants.ACCESS_KEY_ID, Constants.SECRET_KEY_ID);
            string s3Url = client.GetPreSignedURL(new Amazon.S3.Model.GetPreSignedUrlRequest()
            {
                BucketName = "bucket",
                Key = "key",
                VersionId = "v1121",
                Expires = DateTime.Now.AddMinutes(30),
                Verb = HttpVerb.GET
            });

            ApplicationVersion version = new ApplicationVersion(s3Url);

            Assert.AreEqual("bucket", version.S3Bucket);
            Assert.AreEqual("key", version.S3Key);
            Assert.AreEqual("v1121", version.S3Version);
        }

        //[TestMethod]
        public void TestBounceAppPool()
        {
            ServerManager mgr = new ServerManager();
            ApplicationPoolCollection pools = mgr.ApplicationPools;
            
            ApplicationPool defPool = pools["DefaultAppPool"];

            if (defPool != null)
            {
                try
                {
                    defPool.Stop();
                    mgr.CommitChanges();
                }
                catch (Exception e)
                {
                    string foo = e.Message;
                }

                try
                {
                    defPool.Start();
                    mgr.CommitChanges();
                }
                catch (Exception e)
                {
                    string foo = e.Message;
                }
            }
        }

        void makeSureApplicationDoesNotExist(string applicationName)
        {
            ServerManager mgr = new ServerManager();
            var site = mgr.Sites["Default Web Site"];
            if (site.Applications[applicationName] != null)
            {
                site.Applications.Remove(site.Applications[applicationName]);
            }
            mgr.CommitChanges();
        }

        bool doesSiteExist(string applicationName)
        {
            ServerManager mgr = new ServerManager();
            var site = mgr.Sites["Default Web Site"];
            Application app = site.Applications[applicationName];
            return app != null;
        }
    }
}
