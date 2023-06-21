using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public class MetricSources
    {
        public class CredentialProfileFormMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource CredentialProfileForm = new CredentialProfileFormMetricSource("CredentialProfileForm");

            private CredentialProfileFormMetricSource(string location) : base(null, location) { }
        }
    }

}
