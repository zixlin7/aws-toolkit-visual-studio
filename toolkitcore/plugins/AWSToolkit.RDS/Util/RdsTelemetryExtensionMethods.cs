using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.RDS.Util
{
    public static class RdsTelemetryExtensionMethods
    {
        public static void RecordRdsCreateSecurityGroup(this ToolkitContext toolkitContext,
          ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<RdsCreateSecurityGroup>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordRdsCreateSecurityGroup(data);
        }

        public static void RecordRdsCreateSubnetGroup(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<RdsCreateSubnetGroup>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordRdsCreateSubnetGroup(data);
        }

        public static void RecordRdsLaunchInstance(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<RdsLaunchInstance>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordRdsLaunchInstance(data);
        }

        public static void RecordRdsDeleteSecurityGroup(this ToolkitContext toolkitContext, int count,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<RdsDeleteSecurityGroup>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = count;

            toolkitContext.TelemetryLogger.RecordRdsDeleteSecurityGroup(data);
        }

        public static void RecordRdsDeleteSubnetGroup(this ToolkitContext toolkitContext, int count,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<RdsDeleteSubnetGroup>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = count;

            toolkitContext.TelemetryLogger.RecordRdsDeleteSubnetGroup(data);
        }

        public static void RecordRdsDeleteInstance(this ToolkitContext toolkitContext,
          ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<RdsDeleteInstance>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordRdsDeleteInstance(data);
        }
    }
}
