using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.IO;
using System.IO.Pipes;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Tasks;
using AWSDeploymentHostManager.Persistence;

using ThirdParty.Json.LitJson;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class StatusTaskTest
    {
        [TestMethod]
        public void TestStatusWithEvents()
        {
            PersistenceManager.ResetDatabaseForTesting();
            TestUtil.SetHostManagerConfig(new HostManagerConfig(@"{""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}"));
            TaskFactory tf = new TaskFactory();
            tf.RegisterTask("Status", typeof(StatusTask));

            Event.LogWarn("Test", "This is the test message");
            Event.LogInfo("Test", "This is the test message");

            Task statusTask = tf.CreateTaskFromRequest(@"{""name"":""Status""}");

            string response = statusTask.Execute();

            Console.WriteLine("response: {0}", response);

            JsonData jData = JsonMapper.ToObject(response);

            Assert.IsTrue(jData["events"].IsArray);
            Assert.AreEqual<int>(jData["events"].Count, 2);

            int m = 0;
            foreach (JsonData evt in jData["events"])
            {
                Assert.AreEqual<string>("Test", (string)evt["source"]);
                Assert.AreEqual<string>("This is the test message", (string)evt["message"]);

                string sev = (string)evt["severity"];

                if (sev.Equals("info")) m |= 1;
                if (sev.Equals("warn")) m |= 2;
            }

            Assert.AreEqual<int>(3, m);

            Thread.Sleep(1000);

            // The second time through, there should be no events.

            response = statusTask.Execute();

            Console.WriteLine("second response: {0}", response);

            Assert.AreEqual<int>(0, JsonMapper.ToObject(response)["events"].Count);
        }

        [TestMethod]
        public void TestStatusWithPublications()
        {
            PersistenceManager pm = new PersistenceManager();
            TestUtil.SetHostManagerConfig(new HostManagerConfig(@"{""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}"));
            TaskFactory tf = new TaskFactory();

            tf.RegisterTask("Status", typeof(StatusTask));

            FilePublication pending1 = new FilePublication("pending1.txt", @"c:\pending1.txt", @"c:\foo\bar", 1235);
            pending1.SetPending();

            FilePublication pending2 = new FilePublication("pending2.txt", @"c:\pending2.txt", @"c:\foo\bar", 1235);
            pending2.SetPending();

            FilePublication complete = new FilePublication("complete.txt", @"c:\complete.txt", @"c:\foo\bar", 1235);
            complete.SetComplete();

            FilePublication in_progress = new FilePublication("in_progress.txt", @"c:\in_progress.txt", @"c:\foo\bar", 1235);
            in_progress.SetInProcess();

            Task statusTask = tf.CreateTaskFromRequest(@"{""name"":""Status""}");

            string response = statusTask.Execute();

            Console.WriteLine("response: {0}", response);

            JsonData jData = JsonMapper.ToObject(response);

            Assert.AreEqual<int>(2, jData["publications"].Count);

            foreach (JsonData pub in jData["publications"])
            {
                Assert.IsTrue(((string)pub["filename"]).StartsWith("pending"));
                Assert.AreEqual<int>(1235, (int)pub["size"]);
            }
        }

        [TestMethod]
        public void TestStatusWithVersion()
        {
            PersistenceManager.ResetDatabaseForTesting();
            
            PersistenceManager pm = new PersistenceManager();
            TestUtil.SetHostManagerConfig(new HostManagerConfig(@"{""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}"));
            TaskFactory tf = new TaskFactory();

            tf.RegisterTask("Status", typeof(StatusTask));

            EntityObject version0 = new EntityObject(EntityType.ApplicationVersion);
            version0.Parameters.Add("version", "Lalalala");
            version0.Parameters.Add("deployed", "true");

            pm.Persist(version0);

            Thread.Sleep(1000);

            EntityObject version1 = new EntityObject(EntityType.ApplicationVersion);
            version1.Parameters.Add("version", "Bwah-hah-hah");
            version1.Parameters.Add("deployed", "true");

            pm.Persist(version1);

            long ticks = version1.Timestamp.Ticks;

            Task statusTask = tf.CreateTaskFromRequest(@"{""name"":""Status""}");

            string response = statusTask.Execute();

            Console.WriteLine("response: {0}", response);

            JsonData jData = JsonMapper.ToObject(response);

            Assert.AreEqual<string>("Bwah-hah-hah", (string) jData["versions"]["application"]["version"]);
            Assert.AreEqual<long>(ticks, (long)jData["versions"]["application"]["timestamp"]);
            Assert.AreEqual<bool>(true, (bool)jData["versions"]["application"]["deployed"]);

            string hmVersion = (string)jData["versions"]["hostmanager"]["version"];

            Assert.IsNotNull(hmVersion);

            string[] parts = hmVersion.Split(new char[] { '.' });

            foreach (string part in parts)
            {
                int tmp;
                Assert.IsTrue(Int32.TryParse(part, out tmp));
            }
        }

        [TestMethod]
        public void TestLogFileScan()
        {
            PersistenceManager.ResetDatabaseForTesting();
            
            string tmpdir = String.Format("C:\\Temp\\{0}", Guid.NewGuid());
            Directory.CreateDirectory(tmpdir);

            PersistenceManager pm = new PersistenceManager();
            EntityObject timestamp = new EntityObject(EntityType.TimeStamp);
            timestamp.Status = "LogDirectoryScan";
            timestamp.Parameters.Add("path", tmpdir);
            timestamp.Parameters.Add("name", "TempLogs");

            pm.Persist(timestamp);

            Thread.Sleep(1000);

            StreamWriter sr = new StreamWriter(File.Create(String.Format("{0}\\{1}", tmpdir, "File1")));
            sr.WriteLine("One");
            sr.WriteLine("Two");
            sr.WriteLine("Three");
            sr.WriteLine("Four");
            sr.WriteLine("Five");
            sr.WriteLine("Six");
            sr.Close();

            Thread.Sleep(1000);

            sr = new StreamWriter(File.Create(String.Format("{0}\\{1}", tmpdir, "File2")));
            sr.WriteLine("Seven");
            sr.WriteLine("Eight");
            sr.WriteLine("Nine");
            sr.WriteLine("Ten");
            sr.WriteLine("Eleven");
            sr.WriteLine("Twelve");
            sr.Close();

            Thread.Sleep(1000);

            TestUtil.SetHostManagerConfig(new HostManagerConfig(@"{""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}"));
            TaskFactory tf = new TaskFactory();
            tf.RegisterTask("Status", typeof(StatusTask));

            Task statusTask = tf.CreateTaskFromRequest(@"{""name"":""Status""}");
            string response = statusTask.Execute();

            Console.WriteLine("response: {0}", response);

            JsonData jData = JsonMapper.ToObject(response);

            //The log publication is done asyncronously.
            while (jData["publications"].Count != 1)
            {
                response = statusTask.Execute();
                jData = JsonMapper.ToObject(response);
            }

            Assert.AreEqual<int>(1, jData["publications"].Count);
            Assert.AreEqual<string>("File1", (string)jData["publications"][0]["filename"]);
            Assert.AreEqual<string>("TempLogs", (string)jData["publications"][0]["path"]);
           // Assert.IsTrue((long)jData["publications"][0]["size"] > 0);

            FilePublication pub = FilePublication.LoadPendingForS3Name("File1");

            Assert.IsNotNull(pub);

            pub.SetInProcess();

            Thread.Sleep(1000);

            response = statusTask.Execute();

            jData = JsonMapper.ToObject(response);

            Assert.AreEqual<int>(0, jData["publications"].Count);
        }

        [TestMethod]
        public void VerifyLogScanTimestampGetsPrimed()
        {
            PersistenceManager.ResetDatabaseForTesting();

            HostManager hm = new HostManager(@"{""aspLogLocation"":""/Nowhere/In/Particular/"",""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}");
            PersistenceManager pm = new PersistenceManager();

            IList<EntityObject> logDirs = pm.SelectByStatus(EntityType.TimeStamp,"LogDirectoryScan");

            Assert.AreEqual<int>(5, logDirs.Count);

            EntityObject ts = logDirs.Where(eo => String.Equals(eo.Parameters["name"],"IISLogs")).First();

            Assert.IsNotNull(ts);

            Assert.AreEqual<string>("/Nowhere/In/Particular/", ts.Parameters["path"]);
            Assert.AreEqual<string>("LogDirectoryScan", ts.Status);

            // Make sure there is only one after repeated invocations
            hm = new HostManager(@"{""aspLogLocation"":""/Nowhere/In/Particular/"",""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}");
            Assert.AreEqual<int>(5, pm.SelectByStatus(EntityType.TimeStamp, "LogDirectoryScan").Count());
        }
    }
}
