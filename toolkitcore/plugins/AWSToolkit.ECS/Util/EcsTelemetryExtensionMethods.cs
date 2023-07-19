using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ECS.Util
{
    public static class EcsTelemetryExtensionMethods
    {
        public static void RecordEcrCreateRepository(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<EcrCreateRepository>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordEcrCreateRepository(data);
        }

        public static void RecordEcrDeleteRepository(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<EcrDeleteRepository>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordEcrDeleteRepository(data);
        }

        public static void RecordEcsDeleteCluster(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<EcsDeleteCluster>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordEcsDeleteCluster(data);
        }
    }
}
