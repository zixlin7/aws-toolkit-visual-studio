using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using System.Windows;

using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CloudFormation.Util
{
    public static class CloudFormationTelemetryExtensionMethods
    {
        public static void RecordCloudFormationDelete(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<CloudformationDelete>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordCloudformationDelete(data);
        }

        public static void RecordCloudFormationPublishWizard(this ToolkitContext toolkitContext,
            ActionResults result, double duration, AwsConnectionSettings awsConnectionSettings, BaseMetricSource source)
        {
            var data = result.CreateMetricData<CloudformationPublishWizard>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Duration = duration;
            data.Source = source.Location;
            data.ServiceType = source.Service;

            toolkitContext.TelemetryLogger.RecordCloudformationPublishWizard(data);
        }
    }
}
