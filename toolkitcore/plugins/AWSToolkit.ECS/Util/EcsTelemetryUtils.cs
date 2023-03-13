using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages;
using Amazon.AWSToolkit.Telemetry;
using Amazon.Common.DotNetCli.Tools;

using log4net;
using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.ECS.Util
{
    public class EcsTelemetryUtils
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(EcsTelemetryUtils));

        public static EcsLaunchType GetMetricsEcsLaunchType(IAWSWizard hostingWizard)
        {
            try
            {
                if (hostingWizard.IsEc2Launch())
                {
                    return EcsLaunchType.Ec2;
                }
                else if (hostingWizard.IsFargateLaunch())
                {
                    return EcsLaunchType.Fargate;
                }
                else
                {
                    var launchType = hostingWizard[PublishContainerToAWSWizardProperties.LaunchType] as string;
                    _logger.Debug($"Unknown launch type, recorded as unknown: {launchType}");
                    Debug.Assert(false, $"Unsupported ECS Launch Type. Telemetry will be recorded with 'unknown' type: {launchType}");
                    return new EcsLaunchType("unknown");
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error determining ECS Launch Type", e);
                return new EcsLaunchType("unknown");
            }
        }

        public static string GetReason(Exception e)
        {
            if (e is ToolsException toolsException)
            {
                return TelemetryHelper.ConcatenateReasonFragments(toolsException.ServiceCode, toolsException.Code);
            }

            return TelemetryHelper.GetMetricsReason(e);
        }
    }
}
