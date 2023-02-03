using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;
using Amazon.Lambda;

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

        public static void RecordLambdaDeploy(this ITelemetryLogger telemetryLogger, Result deployResult, RecordLambdaDeployProperties lambdaDeploymentProperties)
        {
            telemetryLogger.RecordLambdaDeploy(new LambdaDeploy()
            {
                AwsAccount = lambdaDeploymentProperties.AccountId ?? MetadataValue.Invalid,
                AwsRegion = lambdaDeploymentProperties.RegionId ?? MetadataValue.NotSet,
                Result = deployResult,
                LambdaPackageType = new LambdaPackageType(lambdaDeploymentProperties.LambdaPackageType?.Value ?? MetadataValue.NotSet),
                InitialDeploy = lambdaDeploymentProperties.NewResource,
                Runtime = new AwsToolkit.Telemetry.Events.Generated.Runtime(lambdaDeploymentProperties.Runtime?.Value ?? MetadataValue.NotSet),
                Platform = lambdaDeploymentProperties.TargetFramework,
                LambdaArchitecture = new LambdaArchitecture(lambdaDeploymentProperties.LambdaArchitecture?.Value ?? MetadataValue.NotSet),
                Language = lambdaDeploymentProperties.Language,
                XrayEnabled = lambdaDeploymentProperties.XRayEnabled
            });
        }

        public static void RecordServerlessApplicationDeploy(this ITelemetryLogger telemetryLogger, Result deployResult, string accountId, string regionId, string reason = null)
        {
            telemetryLogger.RecordServerlessapplicationDeploy(
                new ServerlessapplicationDeploy()
                {
                    AwsAccount = accountId ?? MetadataValue.Invalid,
                    AwsRegion = regionId ?? MetadataValue.NotSet,
                    Result = deployResult
                }, metricDatum =>
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        metricDatum.Metadata["reason"] = reason;
                    }

                    return metricDatum;
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
    }
}
