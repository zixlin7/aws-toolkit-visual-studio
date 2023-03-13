using System;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Util;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Utils
{
    public static class BeanstalkTelemetryExtensionMethods
    {
        private static readonly BaseMetricSource _defaultDeployMetricSource = MetricSources.BeanstalkMetricSource.Project;

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

        public static void RecordBeanstalkPublishWizard(this ToolkitContext toolkitContext,
            ActionResults result, double duration, AwsConnectionSettings awsConnectionSettings, BaseMetricSource source = null)
        {
            var metricSource = source ?? _defaultDeployMetricSource;
            var data = result.CreateMetricData<BeanstalkPublishWizard>(awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Duration = duration;
            data.Source = metricSource.Location;
            data.ServiceType = metricSource.Service;

            toolkitContext.TelemetryLogger.RecordBeanstalkPublishWizard(data);
        }
    }
}
