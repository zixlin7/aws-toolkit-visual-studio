using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public class MetricSources
    {
        public class GettingStartedMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource GettingStarted = new GettingStartedMetricSource("AwsGettingStarted");

            private GettingStartedMetricSource(string location) : base(null, location)
            {
            }
        }
    }
}
