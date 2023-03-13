using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class MetricSources
    {
        public class CloudFormationMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource Project = new CloudFormationMetricSource("project");

            private CloudFormationMetricSource(string location) : base(null, location)
            {
            }
        }
    }
}
