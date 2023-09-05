using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.IdentityManagement.Util
{
    public static class IamTelemetryExtensionMethods
    {
        public static void RecordIamCreate(this ToolkitContext toolkitContext, IamResourceType resource,
           ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<IamCreate>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();
            data.IamResourceType = resource;

            toolkitContext.TelemetryLogger.RecordIamCreate(data);
        }

        public static void RecordIamDelete(this ToolkitContext toolkitContext, IamResourceType resource,
           ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<IamDelete>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();
            data.IamResourceType = resource;

            toolkitContext.TelemetryLogger.RecordIamDelete(data);
        }

        public static void RecordIamEdit(this ToolkitContext toolkitContext, IamResourceType resource,
         ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<IamEdit>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();
            data.IamResourceType = resource;

            toolkitContext.TelemetryLogger.RecordIamEdit(data);
        }

        public static void RecordIamCreateAccessKey(this ToolkitContext toolkitContext,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<IamCreateUserAccessKey>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordIamCreateUserAccessKey(data);
        }

        public static void RecordIamDeleteAccessKey(this ToolkitContext toolkitContext,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<IamDeleteUserAccessKey>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordIamDeleteUserAccessKey(data);
        }
    }
}
