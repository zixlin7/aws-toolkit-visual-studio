using System;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
    public class LambdaTesterUsageEmitter
    {
        private readonly ISimpleMobileAnalytics _metrics;
        private int _lastDebugProcessId = -1;

        public LambdaTesterUsageEmitter(ISimpleMobileAnalytics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            this._metrics = metrics;
        }

        public void EmitIfLambdaTester(string processName, int processId)
        {
            if (IsLambdaTester(processName))
            {
                Emit(processId, processName);
            }
        }

        private void Emit(int processId, string processName)
        {
            if (_lastDebugProcessId != processId)
            {
                _lastDebugProcessId = processId;

                AttributeKeys metricKey = AttributeKeys.DotnetLambdaTestToolLaunch_UnknownVersion;
                if (processName.Contains("dotnet-lambda-test-tool-2.1"))
                {
                    metricKey = AttributeKeys.DotnetLambdaTestToolLaunch_2_1;
                }
                else if (processName.Contains("dotnet-lambda-test-tool-3.1"))
                {
                    metricKey = AttributeKeys.DotnetLambdaTestToolLaunch_3_1;
                }

                var evnt = new ToolkitEvent();
                evnt.AddProperty(metricKey, "1");
                _metrics.QueueEventToBeRecorded(evnt);
            }
        }

        public static bool IsLambdaTester(string processName)
        {
            return !string.IsNullOrEmpty(processName) && processName.Contains("dotnet-lambda-test-tool");
        }
    }
}