using System;
using System.Diagnostics;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
    public class LambdaTesterUsageEmitter
    {
        private int _lastDebugProcessId = -1;

        private readonly ITelemetryLogger _telemetryLogger;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LambdaTesterUsageEmitter));

        public LambdaTesterUsageEmitter(ITelemetryLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._telemetryLogger = logger;
        }

        public void EmitIfLambdaTester(string processName, int processId, bool debug)
        {
            if (IsLambdaTester(processName))
            {
                Emit(processId, processName, debug);
            }
        }

        private void Emit(int processId, string processName, bool debug)
        {

            if (_lastDebugProcessId != processId)
            {
                _lastDebugProcessId = processId;
                var runtime = "unknown";
                if (processName.Contains("dotnet-lambda-test-tool-2.1"))
                {
                    runtime = AwsToolkit.Telemetry.Events.Generated.Runtime.Dotnetcore21.ToString();
                }
                else if (processName.Contains("dotnet-lambda-test-tool-3.1"))
                {
                    runtime = AwsToolkit.Telemetry.Events.Generated.Runtime.Dotnetcore31.ToString();
                }
                else if (processName.Contains("dotnet-lambda-test-tool-5.0"))
                {
                    runtime = AwsToolkit.Telemetry.Events.Generated.Runtime.Dotnet50.ToString();
                }
                else if (processName.Contains("dotnet-lambda-test-tool-6.0"))
                {
                    runtime = AwsToolkit.Telemetry.Events.Generated.Runtime.Dotnet6.ToString();
                }
                else if (processName.Contains("dotnet-lambda-test-tool-7.0"))
                {
                    runtime = AwsToolkit.Telemetry.Events.Generated.Runtime.Dotnet7.ToString();
                }
                else if (processName.Contains("dotnet-lambda-test-tool-8.0"))
                {
                    runtime = AwsToolkit.Telemetry.Events.Generated.Runtime.Dotnet8.ToString();
                }

                try
                {
                    _telemetryLogger.RecordLambdaInvokeLocal(new LambdaInvokeLocal()
                    {
                        Runtime = new AwsToolkit.Telemetry.Events.Generated.Runtime(runtime),
                        Debug = debug,
                        Result = Result.Succeeded,
                        LambdaPackageType = LambdaPackageType.Zip
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to record metric", e);
                    Debug.Assert(false, $"Failure recording metric: {e.Message}");
                }
            }
        }

        public static bool IsLambdaTester(string processName)
        {
            return !string.IsNullOrEmpty(processName) && processName.Contains("dotnet-lambda-test-tool");
        }
    }
}
