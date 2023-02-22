using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;
using Amazon.Lambda;

using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Telemetry.Model;

using LambdaArchitecture = Amazon.AwsToolkit.Telemetry.Events.Generated.LambdaArchitecture;

namespace Amazon.AWSToolkit.Lambda.Util
{
    public static class LambdaTelemetryUtils
    {
        public class RecordLambdaDeployProperties
        {
            public bool NewResource { get; set; } = true;
            public Amazon.Lambda.Runtime Runtime { get; set; }
            public string AccountId { get; set; }
            public string RegionId { get; set; }
            public string TargetFramework { get; set; }
            public PackageType LambdaPackageType { get; set; }
            public Architecture LambdaArchitecture { get; set; }
            public string Language { get; set; }
            public bool XRayEnabled { get; set; }
        }

        public static void RecordLambdaDeploy(this ITelemetryLogger telemetryLogger, ActionResults result, double duration, BaseMetricSource metricSource, RecordLambdaDeployProperties lambdaDeploymentProperties)
        {
            telemetryLogger.RecordLambdaDeploy(new LambdaDeploy()
            {
                AwsAccount = lambdaDeploymentProperties.AccountId ?? MetadataValue.Invalid,
                AwsRegion = lambdaDeploymentProperties.RegionId ?? MetadataValue.Invalid,
                Result = result.AsTelemetryResult(),
                LambdaPackageType = new LambdaPackageType(lambdaDeploymentProperties.LambdaPackageType?.Value ?? MetadataValue.NotSet),
                InitialDeploy = lambdaDeploymentProperties.NewResource,
                Runtime = new AwsToolkit.Telemetry.Events.Generated.Runtime(lambdaDeploymentProperties.Runtime?.Value ?? MetadataValue.NotSet),
                Platform = lambdaDeploymentProperties.TargetFramework,
                LambdaArchitecture = new LambdaArchitecture(lambdaDeploymentProperties.LambdaArchitecture?.Value ?? MetadataValue.NotSet),
                Language = lambdaDeploymentProperties.Language,
                XrayEnabled = lambdaDeploymentProperties.XRayEnabled,
                Reason = LambdaHelpers.GetMetricsReason(result?.Exception),
                Duration = duration,
                ServiceType = metricSource?.Service,
                Source = metricSource?.Location
            });
        }

        public static void RecordServerlessApplicationDeploy(this ITelemetryLogger telemetryLogger, ActionResults result, double duration, BaseMetricSource metricSource, string accountId, string regionId)
        {
            telemetryLogger.RecordServerlessapplicationDeploy(new ServerlessapplicationDeploy()
            {
                AwsAccount = accountId ?? MetadataValue.Invalid,
                AwsRegion = regionId ?? MetadataValue.Invalid,
                Result = result.AsTelemetryResult(),
                Reason = LambdaHelpers.GetMetricsReason(result?.Exception),
                Duration = duration,
                ServiceType = metricSource?.Service,
                Source = metricSource?.Location
            });
        }

        public static void RecordLambdaIamRoleCleanup(this ITelemetryLogger telemetryLogger, Result result, string reason, string accountId, string regionId)
        {
            telemetryLogger.RecordLambdaIamRoleCleanup(new LambdaIamRoleCleanup()
            {
                AwsAccount = accountId ?? MetadataValue.Invalid,
                AwsRegion = regionId ?? MetadataValue.NotSet,
                Result = result,
                Reason = reason
            });
        }

        public static void RecordLambdaAddEvent(this ITelemetryLogger telemetryLogger, Result result, string variant, string accountId, string regionId)
        {
            telemetryLogger.RecordLambdaAddEvent(new LambdaAddEvent()
            {
                AwsAccount = accountId ?? MetadataValue.Invalid,
                AwsRegion = regionId ?? MetadataValue.NotSet,
                Result = result,
                Variant = variant
            });
        }

        public static void RecordLambdaDelete(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<LambdaDelete>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Duration = Double.NaN;

            toolkitContext.TelemetryLogger.RecordLambdaDelete(data);
        }

        public static BaseMetricSource AsMetricSource(this UploadFunctionController.UploadOriginator originator)
        {
            switch (originator)
            {
                case UploadFunctionController.UploadOriginator.FromAWSExplorer:
                    return CommonMetricSources.AwsExplorerMetricSource.ServiceNode;
                case UploadFunctionController.UploadOriginator.FromFunctionView:
                    return MetricSources.LambdaMetricSource.LambdaView;
                case UploadFunctionController.UploadOriginator.FromSourcePath:
                    return MetricSources.LambdaMetricSource.Project;
                default:
                    return null;
            }
        }
    }
}
