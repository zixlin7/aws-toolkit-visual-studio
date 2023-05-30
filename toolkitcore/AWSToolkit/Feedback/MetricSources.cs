using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Feedback
{
    public class MetricSources
    {
        public class FeedbackMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource Feedback = new FeedbackMetricSource("Feedback");

            private FeedbackMetricSource(string location) : base(null, location)
            {
            }
        }
    }
}
