using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Models
{

    public class AwsExplorerMetricSource : BaseMetricSource
    {
        public static readonly BaseMetricSource CloudWatchLogsNode = new AwsExplorerMetricSource("AwsExplorer");

        private AwsExplorerMetricSource(string location) : base(null, location)
        {
        }
    }

    public class CloudWatchLogsMetricSource : BaseMetricSource
    {
        public static readonly BaseMetricSource LogGroupsView = new CloudWatchLogsMetricSource("groupsView");
        public static readonly BaseMetricSource LogGroupView = new CloudWatchLogsMetricSource("groupView");

        private CloudWatchLogsMetricSource(string location) : base(ServiceNames.CloudWatchLogs, location)
        {
            
        }
    }
}
