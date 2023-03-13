using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Model
{
    public class MetricSources
    {
        public class CodeCommitMetricSource : BaseMetricSource
        {
            public static readonly BaseMetricSource ConnectPanel = new CodeCommitMetricSource("connectPanel");

            private CodeCommitMetricSource(string location) : base(ServiceNames.CodeCommit, location)
            {
            }
        }
    }
}
