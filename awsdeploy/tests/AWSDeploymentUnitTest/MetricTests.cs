using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeploymentHostManager;
using AWSDeploymentHostManager.Persistence;
using ThirdParty.Json.LitJson;

using System.Threading;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class MetricTests
    {
        [TestMethod]
        public void TestMetricLogging()
        {
            PersistenceManager.ResetDatabaseForTesting();

            Metric.LogCountMetric("UnitTestRate", "100");
            Metric.LogCountMetric("UnitTestSize", "42");
            Metric.LogTimeMetric("UnitTestTime", "00:00:24");

            PersistenceManager pm = new PersistenceManager();
            IList<EntityObject> eos = pm.SelectByGreaterThanTimestamp(EntityType.Metric, DateTime.MinValue);

            Assert.AreEqual(3, eos.Count, "metric count");

            List<Metric> metrics = new List<Metric>();
            foreach (EntityObject eo in eos)
            {
                Metric metric = new Metric(eo);
                metrics.Add(metric);
            }

            Assert.IsNotNull(metrics.FirstOrDefault(m => string.Equals(m.Name, "UnitTestRate") && string.Equals(m.Value, "100") && string.Equals(m.Type, Metric.METRIC_TYPE_COUNT)), "first metric");
            Assert.IsNotNull(metrics.FirstOrDefault(m => string.Equals(m.Name, "UnitTestSize") && string.Equals(m.Value, "42") && string.Equals(m.Type, Metric.METRIC_TYPE_COUNT)), "second metric");
            Assert.IsNotNull(metrics.FirstOrDefault(m => string.Equals(m.Name, "UnitTestTime") && string.Equals(m.Value, "00:00:24") && string.Equals(m.Type, Metric.METRIC_TYPE_TIME)), "third metric");
        }

        [TestMethod]
        public void TestLoadByTimestamp()
        {
            PersistenceManager.ResetDatabaseForTesting();

            DateTime ts1 = DateTime.Now;
            Thread.Sleep(1000);

            Metric.LogCountMetric("FirstGroupCount", "12");
            Metric.LogCountMetric("FirstGroupCount", "13");

            DateTime ts2 = DateTime.Now;
            Thread.Sleep(1000);

            Metric.LogCountMetric("SecondGroupCount", "22");
            Metric.LogCountMetric("SecondGroupCount", "23");

            DateTime ts3 = DateTime.Now;
            Thread.Sleep(1000);

            Metric.LogCountMetric("ThirdGroupCount", "32");
            Metric.LogCountMetric("ThirdGroupCount", "33");

            IList<Metric> metrics = Metric.LoadMetricsSince(ts3);

            Assert.AreEqual<int>(2, metrics.Count);

            foreach (Metric metric in metrics)
            {
                Assert.AreEqual("ThirdGroupCount", metric.Name);
            }

            metrics = Metric.LoadMetricsSince(ts2);

            Assert.AreEqual<int>(4, metrics.Count);

            foreach (Metric metric in metrics)
            {
                Assert.AreNotEqual("FirstGroupCount", metric.Name);
            }

            metrics = Metric.LoadMetricsSince(ts1);
            Assert.AreEqual(6, metrics.Count);
        }

        [TestMethod]
        public void TestJsonOutput()
        {
            Metric time = new Metric(Metric.METRIC_TYPE_TIME, "UnitTestTime", "00:00:24");
            Metric count = new Metric(Metric.METRIC_TYPE_COUNT, "UnitTestCount", "42");

            // Persist them to get the timestamps on.
            time.Persist();
            count.Persist();

            StringBuilder sb = new StringBuilder();
            JsonWriter json = new JsonWriter(sb);

            json.WriteObjectStart();

            json.WritePropertyName("time");
            time.WriteToJson(json);
            json.WritePropertyName("count");
            count.WriteToJson(json);

            json.WriteObjectEnd();

            Console.WriteLine(sb.ToString());

            JsonData jData = JsonMapper.ToObject(sb.ToString());

            Assert.AreEqual(Metric.METRIC_TYPE_TIME, (string)jData["time"]["type"]);
            Assert.AreEqual("UnitTestTime", (string)jData["time"]["name"]);
            Assert.AreEqual("00:00:24", (string)jData["time"]["value"]);

            Assert.AreEqual(Metric.METRIC_TYPE_COUNT, (string)jData["count"]["type"]);
            Assert.AreEqual("UnitTestCount", (string)jData["count"]["name"]);
            Assert.AreEqual("42", (string)jData["count"]["value"]);
        }
    }

    
}
