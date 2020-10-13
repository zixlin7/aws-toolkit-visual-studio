using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;
using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Lambda.Util
{
    public static class LambdaTelemetryUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LambdaTelemetryUtils));

        public class RecordLambdaDeployProperties
        {
            public bool NewResource { get; set; } = true;
            public Amazon.Lambda.Runtime Runtime { get; set; }
            public string RegionId { get; set; }
            public string TargetFramework { get; set; }
        }

        public static void RecordLambdaDeploy(this ITelemetryLogger telemetryLogger, Result deployResult, RecordLambdaDeployProperties lambdaDeploymentProperties)
        {
            try
            {
                telemetryLogger.RecordLambdaDeploy(new LambdaDeploy()
                {
                    InitialDeploy = lambdaDeploymentProperties.NewResource,
                    Result = deployResult,
                    RegionId = lambdaDeploymentProperties.RegionId ?? "unknown",
                    Runtime = new AwsToolkit.Telemetry.Events.Generated.Runtime(
                        lambdaDeploymentProperties.Runtime?.Value ?? "unknown"),
                    Platform = lambdaDeploymentProperties.TargetFramework,
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to record metric", e);
                Debug.Assert(false, $"Failure recording metric: {e.Message}");
            }
        }

        public static void RecordServerlessApplicationDeploy(this ITelemetryLogger telemetryLogger, Result deployResult, string regionId)
        {
            try
            {
                telemetryLogger.RecordServerlessapplicationDeploy(new ServerlessapplicationDeploy()
                {
                    Result = deployResult,
                    RegionId = regionId,
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to record metric", e);
                Debug.Assert(false, $"Failure recording metric: {e.Message}");
            }
        }
    }
}