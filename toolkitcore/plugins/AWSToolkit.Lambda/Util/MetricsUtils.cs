using Amazon.AwsToolkit.Telemetry.Events;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;
using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Lambda.Util
{
    public static class MetricsUtils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LambdaDeploymentMetrics));

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