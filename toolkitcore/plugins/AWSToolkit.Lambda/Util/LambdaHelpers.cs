using System;

using Amazon.AWSToolkit.Telemetry;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.Lambda.Util
{
    public class LambdaHelpers
    {
        public static string GetMetricsReason(Exception e)
        {
            if (e is ToolsException toolsException)
            {
                return TelemetryHelper.ConcatenateReasonFragments(toolsException.ServiceCode, toolsException.Code);
            }

            return TelemetryHelper.GetMetricsReason(e);
        }
    }
}
