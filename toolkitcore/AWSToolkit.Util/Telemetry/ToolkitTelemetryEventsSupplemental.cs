using System.Collections.Generic;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.Telemetry
{
    public static class ToolkitTelemetryEventsSupplemental
    {
        /// Records Telemetry Event:
        /// Called when user exits the Publish to AWS workflow
        public static void RecordPublishEnd(this ITelemetryLogger telemetryLogger, PublishEnd payload, Dictionary<string, object> additionalMetadata)
        {
            try
            {
                var metrics = new Metrics();
                if (payload.CreatedOn.HasValue)
                {
                    metrics.CreatedOn = payload.CreatedOn.Value;
                }
                else
                {
                    metrics.CreatedOn = System.DateTime.Now;
                }
                metrics.Data = new List<MetricDatum>();

                var datum = new MetricDatum();
                datum.MetricName = "publish_end";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("published", payload.Published);

                datum.AddMetadata("duration", payload.Duration);

                foreach (var item in additionalMetadata
                    .Where(item => !datum.Metadata.ContainsKey(item.Key)))
                {
                    datum.AddMetadata(item.Key, item.Value);
                }

                metrics.Data.Add(datum);
                telemetryLogger.Record(metrics);
            }
            catch (System.Exception e)
            {
                telemetryLogger.Logger.Error("Error recording telemetry event", e);
                System.Diagnostics.Debug.Assert(false, "Error Recording Telemetry");
            }
        }
    }

    /// Called when user exits the Publish to AWS workflow
    public sealed class PublishEnd : BaseTelemetryEvent
    {

        /// Whether or not the user published an application
        public bool Published;

        /// The duration of the operation in milliseconds
        public double Duration;

        public PublishEnd()
        {
            this.Passive = false;
        }
    }
}
