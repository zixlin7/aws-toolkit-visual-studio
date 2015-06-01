using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

using log4net;
using ThirdParty.Json.LitJson;
using AWSDeploymentHostManager.Persistence;

namespace AWSDeploymentHostManager.Tasks
{
    public class EventsTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(EventsTask));
        private const string JSON_KEY_EVENTS = "events";

        private const string START_PARAM = "StartTime";
        private const string END_PARAM = "EndTime";

        public override string Operation
        {
            get
            {
                return "Events";
            }
        }

        public override string Execute()
        {
            LOGGER.Info("Execute");

            StringBuilder sb = new StringBuilder();
            JsonWriter json = new JsonWriter(sb);

            json.WriteObjectStart();

            EmitEvents(json);

            json.WriteObjectEnd();
            return sb.ToString();
        }

        private void EmitEvents(JsonWriter json)
        {
            json.WritePropertyName(JSON_KEY_EVENTS);
            json.WriteArrayStart();

            DateTime startTime = DateTime.MinValue;
            DateTime endTime = DateTime.MaxValue;

            string temp;
            if (this.parameters.TryGetValue(START_PARAM, out temp))
                startTime = DateTime.Parse(temp);
            if (this.parameters.TryGetValue(END_PARAM, out temp))
                endTime = DateTime.Parse(temp);

            LOGGER.DebugFormat("Selecting events from {0} to {1}", startTime, endTime);
            IList<Event> eventsToSend = Event.LoadEventsByRange(startTime, endTime);

            foreach (Event evt in eventsToSend)
            {
                evt.WriteToJson(json);
            }

            json.WriteArrayEnd();
        }
    }
}
