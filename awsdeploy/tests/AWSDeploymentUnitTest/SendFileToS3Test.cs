
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Persistence;
using AWSDeploymentHostManager.Tasks;
using ThirdParty.Json.LitJson;

using Amazon.S3;
using Amazon.S3.Model;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class SendFileToS3Test
    {
        [TestMethod]
        public void UploadFile()
        {
            PersistenceManager.ResetDatabaseForTesting();
            string bucketName = "AWSBeanstalkUnitTest" + DateTime.Now.Ticks;
            string keyName = "test";
            var client = new AmazonS3Client(Constants.ACCESS_KEY_ID, Constants.SECRET_KEY_ID);

            string filename = createFile();
            client.PutBucket(new PutBucketRequest(){BucketName = bucketName});
            try
            {
                string s3url = client.GetPreSignedURL(new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentType = "text/css",
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.Now.AddDays(10)
                });

                var task = new SendFileToS3Task();
                task.SetParameter("s3url", s3url);
                task.SetParameter("filename", filename);
                task.SetParameter("content-type", "text/css");

                PersistenceManager pm = new PersistenceManager();
                FileInfo fi = new FileInfo(filename);
                FilePublication pub = new FilePublication(keyName, fi.FullName, bucketName, fi.Length);
                pub.Persist();

                DateTime start = DateTime.Now;

                Thread.Sleep(100);

                string response = task.Execute();

                Console.WriteLine("Response: {0}", response);

                JsonData json = JsonMapper.ToObject(response);

                Assert.AreEqual<string>(task.Operation, (string)json["operation"]);
                Assert.AreEqual<string>("deferred", (string)json["response"]);


                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(1000);
                    IList<Event> events = Event.LoadEventsSince(start);

                    foreach (Event evt in events)
                    {
                        Console.WriteLine("Event[{0}]: {1}", evt.Severity, evt.Message);
                    }

                    if (events.Count > 0)
                        break;
                }

                using (var getResponse = client.GetObject(new Amazon.S3.Model.GetObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName
                }))
                {
                    Assert.AreEqual("text/css", getResponse.Headers.ContentType);
                }
            }
            finally
            {
                File.Delete(filename);
                try
                {
                    client.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest()
                    {
                        BucketName = bucketName,
                        Key = keyName
                    });
                }
                catch { }
                client.DeleteBucket(new DeleteBucketRequest() { BucketName = bucketName });
            }
        }

        static string createFile()
        {
            if (!Directory.Exists(@"C:\temp"))
                Directory.CreateDirectory(@"C:\temp");

            string filename = @"C:\temp\SendFileToS3Task_" + DateTime.Now.Ticks;
            File.WriteAllText(filename, "ImportantData");
            return filename;
        }
    }
}
