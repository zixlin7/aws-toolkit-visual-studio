using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Pipes;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Persistence;
using AWSDeploymentHostManager.Tasks;
using ThirdParty.Json.LitJson;

using System.Threading;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class EventTests
    {
        [TestMethod]
        public void TestEventLogging()
        {
            PersistenceManager.ResetDatabaseForTesting();

            Event.LogInfo("UnitTest", "Information");
            Event.LogWarn("UnitTest", "Warning");
            Event.LogCritical("UnitTest", "Critical");

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Assert.AreEqual<int>(3, eos.Count);

            int match = 0;
            foreach (EntityObject eo in eos)
            {
                Event evt = new Event(eo);

                Assert.AreEqual<string>("UnitTest", evt.Source);

                if (evt.Severity == Event.EVENT_SEVERITY_INFO)
                    match |= 1;
                if (evt.Severity == Event.EVENT_SEVERITY_WARN)
                    match |= 2;
                if (evt.Severity == Event.EVENT_SEVERITY_CRIT)
                    match |= 4;
            }

            Assert.AreEqual<int>(7, match);
        }

        [TestMethod]
        public void TestLoadByTimestamp()
        {
            PersistenceManager.ResetDatabaseForTesting();

            DateTime ts1 = DateTime.Now;
            Thread.Sleep(1000);

            Event.LogInfo("FirstGroup", "Something happened.");
            Event.LogInfo("FirstGroup", "Something happened.");

            DateTime ts2 = DateTime.Now;
            Thread.Sleep(1000);

            Event.LogInfo("SecondGroup", "Something happened.");
            Event.LogInfo("SecondGroup", "Something happened.");

            DateTime ts3 = DateTime.Now;
            Thread.Sleep(1000);

            Event.LogInfo("ThirdGroup", "Something happened.");
            Event.LogInfo("ThirdGroup", "Something happened.");

            IList<Event> events = Event.LoadEventsSince(ts3);

            Assert.AreEqual<int>(2, events.Count);

            foreach (Event evt in events)
            {
                Assert.AreEqual<string>("ThirdGroup", evt.Source);
            }

            events = Event.LoadEventsSince(ts2);

            Assert.AreEqual<int>(4, events.Count);

            foreach (Event evt in events)
            {
                Assert.AreNotEqual<string>("FirstGroup", evt.Source);
            }

            events = Event.LoadEventsSince(ts1);

            Assert.AreEqual<int>(6, events.Count);
        }

        [TestMethod]
        public void TestJsonOutput()
        {
            Event info = new Event("UnitTest", "info");
            Event warn = new Event("UnitTest", "warn", Event.EVENT_SEVERITY_WARN);
            Event crit = new Event("UnitTest", "crit", Event.EVENT_SEVERITY_CRIT);

            // Persist them to get the timestamps on.
            info.Persist();
            warn.Persist();
            crit.Persist();

            StringBuilder sb = new StringBuilder();
            JsonWriter json = new JsonWriter(sb);

            json.WriteObjectStart();

            json.WritePropertyName("info");
            info.WriteToJson(json);
            json.WritePropertyName("warn");
            warn.WriteToJson(json);
            json.WritePropertyName("crit");
            crit.WriteToJson(json);

            json.WriteObjectEnd();

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.AreEqual<string>("UnitTest", (string)jData["info"]["source"]);
            Assert.AreEqual<string>("UnitTest", (string)jData["warn"]["source"]);
            Assert.AreEqual<string>("UnitTest", (string)jData["crit"]["source"]);

            Assert.AreEqual<string>("info", (string)jData["info"]["message"]);
            Assert.AreEqual<string>("warn", (string)jData["warn"]["message"]);
            Assert.AreEqual<string>("crit", (string)jData["crit"]["message"]);

            Assert.AreEqual<string>(Event.EVENT_SEVERITY_INFO, (string)jData["info"]["severity"]);
            Assert.AreEqual<string>(Event.EVENT_SEVERITY_WARN, (string)jData["warn"]["severity"]);
            Assert.AreEqual<string>(Event.EVENT_SEVERITY_CRIT, (string)jData["crit"]["severity"]);

            Console.WriteLine((string)jData["info"]["timestamp"]);

            Assert.IsNotNull((string)jData["info"]["timestamp"]);
            Assert.IsNotNull((string)jData["warn"]["timestamp"]);
            Assert.IsNotNull((string)jData["crit"]["timestamp"]);
        }

        [TestMethod]
        public void TestTaggedEvents()
        {
            PersistenceManager.ResetDatabaseForTesting();
            Event taggedEvent = new Event("TestSource", "TestMessage");
            taggedEvent.Tags.Add("foo");
            taggedEvent.Tags.Add("bar");
            taggedEvent.Persist();

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Event evt = new Event(eos[0]);

            Assert.IsNotNull(evt);

            Assert.AreEqual<int>(2, evt.Tags.Count);
            Assert.IsTrue(evt.Tags.Contains("foo"));
            Assert.IsTrue(evt.Tags.Contains("bar"));

            StringBuilder sb = new StringBuilder();
            JsonWriter jw = new JsonWriter(sb);
            evt.WriteToJson(jw);

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.AreEqual<int>(2, jData[Event.JSON_KEY_TAGS].Count);
            Assert.AreEqual<string>("foo", (string)jData[Event.JSON_KEY_TAGS][0]);
            Assert.AreEqual<string>("bar", (string)jData[Event.JSON_KEY_TAGS][1]);
        }

        [TestMethod]
        public void TestUnTaggedEvents()
        {
            PersistenceManager.ResetDatabaseForTesting();
            Event unTagged = new Event("TestSource", "TestMessage");
            unTagged.Persist();

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Event evt = new Event(eos[0]);

            Assert.IsNotNull(evt);

            Assert.AreEqual<int>(0, evt.Tags.Count);

            StringBuilder sb = new StringBuilder();
            JsonWriter jw = new JsonWriter(sb);
            evt.WriteToJson(jw);

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.IsNull(jData[Event.JSON_KEY_TAGS]);
        }

        [TestMethod]
        public void TestNotVisible()
        {
            PersistenceManager.ResetDatabaseForTesting();

            Event notVisible = new Event("TestSource", "TestMessage");
            notVisible.IsCustomerVisible = false;
            notVisible.Persist();

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Event evt = new Event(eos[0]);

            Assert.IsNotNull(evt);

            Assert.IsFalse(evt.IsCustomerVisible);

            StringBuilder sb = new StringBuilder();
            JsonWriter jw = new JsonWriter(sb);
            evt.WriteToJson(jw);

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.IsFalse((bool)jData[Event.JSON_KEY_VISIBLE]);
        }

        [TestMethod]
        public void TestVisbleExplicit()
        {
            PersistenceManager.ResetDatabaseForTesting();

            Event visible = new Event("TestSource", "TestMessage");
            visible.IsCustomerVisible = true;
            visible.Persist();

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Event evt = new Event(eos[0]);

            Assert.IsNotNull(evt);

            Assert.IsTrue(evt.IsCustomerVisible);

            StringBuilder sb = new StringBuilder();
            JsonWriter jw = new JsonWriter(sb);
            evt.WriteToJson(jw);

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.IsNull(jData[Event.JSON_KEY_VISIBLE]);
        }

        [TestMethod]
        public void TestVisbleDefault()
        {
            PersistenceManager.ResetDatabaseForTesting();

            Event plainEvent = new Event("TestSource", "TestMessage");
            plainEvent.Persist();

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Event evt = new Event(eos[0]);

            Assert.IsNotNull(evt);

            Assert.IsTrue(evt.IsCustomerVisible);

            StringBuilder sb = new StringBuilder();
            JsonWriter jw = new JsonWriter(sb);
            evt.WriteToJson(jw);

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.IsNull(jData[Event.JSON_KEY_VISIBLE]);
        }

        [TestMethod]
        public void TestMilestoneEvents()
        {
            PersistenceManager.ResetDatabaseForTesting();
            Event.LogMilestone("MilestoneSource", "So... this happened", "bling", "blang");

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Event, DateTime.MinValue);

            Event evt = new Event(eos[0]);

            Assert.IsNotNull(evt);

            Assert.IsFalse(evt.IsCustomerVisible);
            Assert.IsTrue(evt.Tags.Contains(Event.EVENT_TAG_MILESTONE));

            Assert.IsFalse(evt.IsCustomerVisible);

            StringBuilder sb = new StringBuilder();
            JsonWriter jw = new JsonWriter(sb);
            evt.WriteToJson(jw);

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.IsFalse((bool)jData[Event.JSON_KEY_VISIBLE]);
            Assert.AreEqual<string>(Event.EVENT_TAG_MILESTONE, (string)jData[Event.JSON_KEY_TAGS][0]);
        }

        [TestMethod]
        public void TestLastHealthcheckTimestamp()
        {
            PersistenceManager.ResetDatabaseForTesting();
            var last = HostManager.LastHealthcheckTransition;
            Assert.AreEqual("Start", last.Parameters[HostManager.LAST_HEALTHCHECK_CODE]);

            last.Parameters[HostManager.LAST_HEALTHCHECK_CODE] = "200";
            new PersistenceManager().Persist(last);

            Assert.AreEqual("200", HostManager.LastHealthcheckTransition.Parameters[HostManager.LAST_HEALTHCHECK_CODE]);
        }

        [TestMethod]
        public void TestHostManagerStartMilestones()
        {
            PersistenceManager.ResetDatabaseForTesting();
            HostManager hm = new HostManager(@"{""ec2InstanceId"":""foo"", ""ec2ReservationId"":""bar""}");

            TaskFactory tf = new TaskFactory();
            tf.RegisterTask("Status", typeof(StatusTask));

            Task statusTask = tf.CreateTaskFromRequest(@"{""name"":""Status""}");
            string response = statusTask.Execute();

            Console.WriteLine("response: {0}", response);

            JsonData jData = JsonMapper.ToObject(response);

            Assert.IsTrue(jData["events"].Count >= 2);

            int match = 0;

            foreach (JsonData evt in jData["events"])
            {
                if (((string)evt["source"]).Equals("host_manager") && ((string)evt["message"]).StartsWith("Starting"))
                    match |= 1;
                if (((string)evt["source"]).Equals("host_manager") && ((string)evt["message"]).StartsWith("Host Manager startup"))
                    match |= 2;
            }

            Assert.AreEqual<int>(3, match);
        }
    }
}
