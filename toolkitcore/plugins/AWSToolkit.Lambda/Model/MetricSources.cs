using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class MetricSources
    {
        public class LambdaMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource Project = new LambdaMetricSource(null, "project");
            public static readonly BaseMetricSource LambdaNode = new LambdaMetricSource("lambdaNode");
            public static readonly BaseMetricSource LambdaView = new LambdaMetricSource("lambdaView");

            private LambdaMetricSource(string location) : this(ServiceNames.Lambda, location)
            {
            }

            private LambdaMetricSource(string service, string location) : base(service, location)
            {
            }
        }
    }
}
