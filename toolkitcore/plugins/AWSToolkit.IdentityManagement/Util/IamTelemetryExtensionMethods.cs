using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.IdentityManagement.Util
{
    public static class IamTelemetryExtensionMethods
    {
        public static void RecordIamCreate(this ToolkitContext toolkitContext, IamResourceType resource,
           ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new IamCreate()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                IamResourceType = resource,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordIamCreate(data);
        }

        public static void RecordIamDelete(this ToolkitContext toolkitContext, IamResourceType resource,
           ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new IamDelete()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                IamResourceType = resource,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordIamDelete(data);
        }

        public static void RecordIamEdit(this ToolkitContext toolkitContext, IamResourceType resource,
         ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new IamEdit()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                IamResourceType = resource,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordIamEdit(data);
        }

        public static void RecordIamCreateAccessKey(this ToolkitContext toolkitContext,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new IamCreateUserAccessKey()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordIamCreateUserAccessKey(data);
        }

        public static void RecordIamDeleteAccessKey(this ToolkitContext toolkitContext,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new IamDeleteUserAccessKey()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordIamDeleteUserAccessKey(data);
        }
    }
}
