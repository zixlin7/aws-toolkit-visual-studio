using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class MetricSources
    {
        public class CloudWatchLogsMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource ClusterTaskView = new CloudWatchLogsMetricSource("clusterTaskView");

            private CloudWatchLogsMetricSource(string location) : base(ServiceNames.Ecs, location)
            {
            }
        }
    }
}
