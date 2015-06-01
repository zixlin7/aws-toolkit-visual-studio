using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeploymentHostManager.Persistence;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    public class Metric
    {
        #region Consts
        
        public const string
            METRIC_TYPE_TIME = "time",
            METRIC_TYPE_COUNT = "count";

        public const string
            JSON_KEY_TYPE = "type",
            JSON_KEY_NAME = "name",
            JSON_KEY_VALUE = "value";
        
        #endregion

        #region Private members

        private EntityObject metric;

        #endregion

        #region Static log methods

        public static void LogCountMetric(string name, string value)
        {
            LogMetric(METRIC_TYPE_COUNT, name, value);
        }
        public static void LogTimeMetric(string name, string value)
        {
            LogMetric(METRIC_TYPE_TIME, name, value);
        }
        private static void LogMetric(string type, string name, string value)
        {
            var metric = new Metric(type, name, value);
            metric.Persist();
        }

        public static IList<Metric> LoadMetricsSince(DateTime timestamp)
        {
            IList<Metric> metrics = new List<Metric>();
            PersistenceManager pm = new PersistenceManager();

            IList<EntityObject> entities = pm.SelectByGreaterThanTimestamp(EntityType.Metric, timestamp);

            foreach (EntityObject eo in entities)
            {
                metrics.Add(new Metric(eo));
            }

            return metrics;
        }

        #endregion

        #region Constructors

        public Metric(string type, string name, string value)
        {
            metric = new EntityObject(EntityType.Metric);
            Type = type;
            Name = name;
            Value = value;
        }

        public Metric(EntityObject eo)
        {
            this.metric = eo;
        }

        #endregion

        #region Public methods
        
        public void WriteToJson(JsonWriter json)
        {
            json.WriteObjectStart();

            json.WritePropertyName(Task.JSON_KEY_TIMESTAMP);
            json.Write(metric.Timestamp.ToUniversalTime().ToString(Task.JSON_TIMESTAMP_FORMAT));

            if (metric.Parameters != null)
            {
                foreach (var kvp in metric.Parameters)
                {
                    json.WritePropertyName(kvp.Key);
                    json.Write(kvp.Value);
                }
            }

            json.WriteObjectEnd();
        }

        public void Persist()
        {
            PersistenceManager pm = new PersistenceManager();
            pm.Persist(metric);
        }

        #endregion

        #region Properties

        public string Type
        {
            get
            {
                return ParameterGet(JSON_KEY_TYPE);
            }
            set
            {
                ParameterSet(JSON_KEY_TYPE, value);
            }
        }

        public string Name
        {
            get
            {
                return ParameterGet(JSON_KEY_NAME);
            }
            set
            {
                ParameterSet(JSON_KEY_NAME, value);
            }
        }

        public string Value
        {
            get
            {
                return ParameterGet(JSON_KEY_VALUE);
            }
            set
            {
                ParameterSet(JSON_KEY_VALUE, value);
            }
        }

        #endregion

        #region Private methods

        private void ParameterSet(string name, string value)
        {
            metric.Parameters[name] = value;
        }
        private string ParameterGet(string name)
        {
            string value;
            if (!metric.Parameters.TryGetValue(name, out value))
                value = null;
            return value;
        }

        #endregion
    }
}
