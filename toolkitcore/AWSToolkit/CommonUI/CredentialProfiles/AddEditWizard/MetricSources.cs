using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class MetricSources
    {
        public class AddEditProfileWizardViewModelMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource AddEditProfileWizard = new AddEditProfileWizardViewModelMetricSource("AddEditProfileWizard");

            private AddEditProfileWizardViewModelMetricSource(string location) : base(null, location) { }
        }
    }
}
