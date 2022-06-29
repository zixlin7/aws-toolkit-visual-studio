using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class MetricSources
    {
        public class CloudWatchLogsMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource LambdaNode = new CloudWatchLogsMetricSource("lambdaNode");
            public static readonly BaseMetricSource LambdaView = new CloudWatchLogsMetricSource("lambdaView");

            private CloudWatchLogsMetricSource(string location) : base(ServiceNames.Lambda, location)
            {
            }
        }
    }
}
