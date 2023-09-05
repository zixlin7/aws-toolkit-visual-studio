using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public class MetricSources
    {
        public class GettingStartedMetricSource : BaseMetricSource
        {
            // This should be used when the Getting Started page is opened from the Extensions main menu item
            public static readonly BaseMetricSource GettingStarted = new GettingStartedMetricSource("AwsGettingStarted");

            // This should be used on the first run of the toolkit when the Getting Started page is opened from
            // AWSToolkitPackage.ShowFirstRun
            public static readonly BaseMetricSource FirstStartup = new GettingStartedMetricSource("firstStartup");

            private GettingStartedMetricSource(string location) : base(null, location)
            {
            }
        }
    }
}
