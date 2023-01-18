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
    }
}
