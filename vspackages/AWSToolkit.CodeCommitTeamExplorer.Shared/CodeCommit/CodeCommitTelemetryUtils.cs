using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

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
            var payload = results.CreateMetricData<CodecommitSetCredentials>(GetAccountId(), GetRegionId());
            payload.Result = results.AsTelemetryResult();

            ToolkitFactory.Instance.TelemetryLogger.RecordCodecommitSetCredentials(payload);
        }

        public static void RecordOpenUrl(ActionResults results, string url)
        {
            var payload = results.CreateMetricData<AwsOpenUrl>(GetAccountId(), GetRegionId());
            payload.Url = url;
            payload.Result = results.AsTelemetryResult();

            ToolkitFactory.Instance.TelemetryLogger.RecordAwsOpenUrl(payload);

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
