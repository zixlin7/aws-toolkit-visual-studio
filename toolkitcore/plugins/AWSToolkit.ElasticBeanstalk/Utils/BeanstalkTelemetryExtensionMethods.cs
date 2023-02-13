using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Utils
{
    public static class BeanstalkTelemetryExtensionMethods
    {
        public static void RecordBeanstalkDeleteEnvironment(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<BeanstalkDeleteEnvironment>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordBeanstalkDeleteEnvironment(data);
        }

        public static void RecordBeanstalkDeleteApplication(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<BeanstalkDeleteApplication>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordBeanstalkDeleteApplication(data);
        }

        public static void RecordBeanstalkRestartApplication(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<BeanstalkRestartApplication>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordBeanstalkRestartApplication(data);
        }

        public static void RecordBeanstalkRebuildEnvironment(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<BeanstalkRebuildEnvironment>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordBeanstalkRebuildEnvironment(data);
        }

        public static void RecordBeanstalkEditEnvironment(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<BeanstalkEditEnvironment>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordBeanstalkEditEnvironment(data);
        }
    }
}
