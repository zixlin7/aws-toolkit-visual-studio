using System.Collections.Generic;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;


namespace Amazon.AWSToolkit.Telemetry
{
    public static class ToolkitTelemetryEventsSupplemental
    {
        // https://github.com/aws/aws-toolkit-common/blob/de3040907568562e3957145a0c90fe9263c87ed1/telemetry/service/service-model.json#L232
        public const int MetricDatumValueMaxLength = 200;

        public static void SplitAndAddMetadata(this MetricDatum @this, string key, string value, int length = MetricDatumValueMaxLength)
        {
            // Split metric field up as telemetry service has a 200 char limit per field
            foreach (var pair in value.SplitByLength(length).Select((str, index) => new { str, index }))
            {
                // Rather than key0, emit key without a suffix for the first segment so Elasticsearch queries that cross multiple metrics
                // that don't all have split fields will be more natural and just work to get up to the first 200 chars
                @this.AddMetadata(pair.index == 0 ? key : key + pair.index, pair.str);
            }
        }
    }
}
