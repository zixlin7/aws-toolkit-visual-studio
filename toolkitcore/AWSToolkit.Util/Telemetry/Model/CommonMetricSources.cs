namespace Amazon.AWSToolkit.Telemetry.Model
{
    public class CommonMetricSources
    {
        public class AwsExplorerMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource ServiceNode = new AwsExplorerMetricSource("AwsExplorer");

            private AwsExplorerMetricSource(string location) : base(null, location)
            {
            }
        }
    }
}
