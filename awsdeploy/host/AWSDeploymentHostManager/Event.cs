using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AWSDeploymentHostManager.Persistence;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    public class Event
    {
        public const string
            EVENT_SEVERITY_WARN = "warn",
            EVENT_SEVERITY_INFO = "info",
            EVENT_SEVERITY_CRIT = "critical";

        public const string
            JSON_KEY_SOURCE = "source",
            JSON_KEY_MESSAGE = "message",
            JSON_KEY_SEVERITY = "severity",
            JSON_KEY_TAGS = "tags",
            JSON_KEY_VISIBLE = "customer_visible";

        public const string
            EVENT_TAG_MILESTONE = "milestone";

        private EntityObject evt;
        private IList<string> tags;

        public static void LogEvent(string source, string message, string severity)
        {
            LogEvent(source, message, severity, null);
        }

        public static void LogEvent(string source, string message, string severity, IDictionary<string, string> extraFields)
        {
            var evt = new Event(source, message, severity);
            if (extraFields != null)
            {
                foreach (KeyValuePair<string, string> kvp in extraFields)
                {
                    evt.ExtraParameters[kvp.Key] = kvp.Value;
                }
            }
            evt.Persist();
        }

        public static void LogEvent(Event evt)
        {
            evt.Persist();
        }

        public static void LogInfo(string source, string message)
        {
            LogEvent(source, message, EVENT_SEVERITY_INFO);
        }

        public static void LogWarn(string source, string message)
        {
            LogEvent(source, message, EVENT_SEVERITY_WARN);
        }

        public static void LogCritical(string source, string message)
        {
            LogEvent(source, message, EVENT_SEVERITY_CRIT);
        }

        public static void LogMilestone(string source, string message, params string[] tags)
        {
            Event evt = new Event(source, message, EVENT_SEVERITY_INFO);
            evt.Tags.Add(EVENT_TAG_MILESTONE);
            evt.IsCustomerVisible = false;
            if (tags != null)
            {
                foreach (string t in tags)
                    evt.Tags.Add(t);
            }
            evt.Persist();
        }

        public static IList<Event> LoadEventsSince(DateTime timestamp)
        {
            IList<Event> events = new List<Event>();
            PersistenceManager pm = new PersistenceManager();

            IList<EntityObject> entities = pm.SelectByGreaterThanTimestamp(EntityType.Event, timestamp);

            foreach (EntityObject eo in entities)
            {
                events.Add(new Event(eo));
            }

            return events;
        }

        public static IList<Event> LoadEventsByRange(DateTime startTime, DateTime endTime)
        {
            IList<Event> events = new List<Event>();
            PersistenceManager pm = new PersistenceManager();

            IList<EntityObject> entities = pm.SelectByTimestampRange(EntityType.Event, startTime, endTime);

            foreach (EntityObject eo in entities)
            {
                events.Add(new Event(eo));
            }

            return events;
        }

        public Event(string source, string message, string severity)
        {
            evt = new EntityObject(EntityType.Event);
            Source = source;
            Message = message;
            Severity = severity;
        }

        public Event(string source, string message) : this (source, message, EVENT_SEVERITY_INFO) {}

        public Event(EntityObject eo)
        {
            this.evt = eo;
        }

        public void WriteToJson(JsonWriter json)
        {
            json.WriteObjectStart();

            json.WritePropertyName(Task.JSON_KEY_TIMESTAMP);
            json.Write(evt.Timestamp.ToUniversalTime().ToString(Task.JSON_TIMESTAMP_FORMAT));

            json.WritePropertyName(JSON_KEY_SOURCE);
            json.Write(Source);

            json.WritePropertyName(JSON_KEY_MESSAGE);
            json.Write(Message);

            json.WritePropertyName(JSON_KEY_SEVERITY);
            json.Write(Severity);

            if (Tags.Count > 0)
            {
                json.WritePropertyName(JSON_KEY_TAGS);
                TagsToJson(json);
            }

            if (!IsCustomerVisible)
            {
                json.WritePropertyName(JSON_KEY_VISIBLE);
                json.Write(IsCustomerVisible);
            }

            json.WriteObjectEnd();
        }

        public void Persist()
        {
            TagsToJson();
            PersistenceManager pm = new PersistenceManager();
            pm.Persist(evt);
        }

        public string Source
        {
            get
            {
                string source = null;
                evt.Parameters.TryGetValue(JSON_KEY_SOURCE, out source);
                return source;
            }
            set
            {
                evt.Parameters[JSON_KEY_SOURCE] = value;
            }
        }

        public string Message
        {
            get
            {
                string message = null;
                evt.Parameters.TryGetValue(JSON_KEY_MESSAGE, out message);
                return message;
            }
            set
            {
                evt.Parameters[JSON_KEY_MESSAGE] = value;
            }
        }

        public string Severity
        {
            get
            {
                string severity = null;
                evt.Parameters.TryGetValue(JSON_KEY_SEVERITY, out severity);
                return severity;
            }
            set
            {
                evt.Parameters[JSON_KEY_SEVERITY] = value;
            }
        }

        public bool IsCustomerVisible
        {
            get
            {
                bool visible = true;
                string vString = null;
                if (evt.Parameters.TryGetValue(JSON_KEY_VISIBLE, out vString))
                {
                    bool.TryParse(vString, out visible);
                }
                return visible;
            }
            set
            {
                evt.Parameters[JSON_KEY_VISIBLE] = value.ToString();
            }
        }

        public IList<string> Tags
        {
            get
            {
                if (null == tags)
                {
                    tags = HydrateTagsFromJason();
                }
                return tags;
            }
        }

        private IList<string> HydrateTagsFromJason()
        {
            var tags = new List<string>();
            string jTags;
            if (evt.Parameters.TryGetValue(JSON_KEY_TAGS, out jTags))
            {
                JsonData tagData = JsonMapper.ToObject(jTags);
                for(int i = 0; i<tagData.Count; i++)
                {
                    tags.Add((string)tagData[i]);
                }
            }
        
            return tags;
        }

        private void TagsToJson()
        {
            if (Tags.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                JsonWriter jw = new JsonWriter(sb);
                TagsToJson(jw);
                ExtraParameters.Add(JSON_KEY_TAGS, sb.ToString());
            }
        }

        private void TagsToJson(JsonWriter writer)
        {
            writer.WriteArrayStart();
            foreach (string tag in Tags)
            {
                writer.Write(tag);
            }
            writer.WriteArrayEnd();
        }

        public IDictionary<string, string> ExtraParameters
        {
            get
            {
                if (evt.Parameters == null)
                    evt.Parameters = new Dictionary<string, string>();

                return evt.Parameters;
            }
        }
    }
}
