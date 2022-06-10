using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CloudWatch.Models
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
        // todo : define entries for opening stream, group

        private CloudWatchLogsMetricSource(string location) : base(ServiceNames.CloudWatchLogs, location)
        {
            
        }
    }
}
