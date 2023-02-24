using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit
{
    public static class CodeCommitTelemetryUtils
    {
        public static void RecordCodeCommitCreateRepoMetric(bool success, string reason)
        {
            var payload = new CodecommitCreateRepo
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Result = success ? Result.Succeeded : Result.Failed
            };

            if (!success)
            {
                payload.Reason = reason;
            }

            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitCreateRepo(payload);
        }

        public static void RecordCodeCommitCloneRepoMetric(bool success, string reason)
        {
            var payload = new CodecommitCloneRepo
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Result = success ? Result.Succeeded : Result.Failed
            };

            if (!success)
            {
                payload.Reason = reason;
            }

            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitCloneRepo(payload);
        }

        public static void RecordCodeCommitSetCredentialsMetric(ActionResults results)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitSetCredentials(new CodecommitSetCredentials
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results?.Exception)
            });
        }

        public static void RecordOpenUrl(ActionResults results, string url)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordAwsOpenUrl(new AwsOpenUrl()
            {
                AwsAccount = GetAccountId(),
                AwsRegion = GetRegionId(),
                Url = url,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results?.Exception)
            });
        }

        private static string GetAccountId()
        {
            return TeamExplorerConnection.ActiveConnection?.AccountId ?? MetadataValue.NotSet;
        }

        private static string GetRegionId()
        {
            return TeamExplorerConnection.ActiveConnection?.Account.Region?.Id ?? MetadataValue.NotSet;
        }
    }
}
