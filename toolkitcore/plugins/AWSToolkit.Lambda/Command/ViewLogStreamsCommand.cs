using System;

using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry.Model;

using log4net;

namespace Amazon.AWSToolkit.Lambda.Command
{
    public class ViewLogStreamsCommand : BaseConnectionContextCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogStreamsCommand));
        private static readonly BaseMetricSource ViewLogsMetricSource = MetricSources.LambdaMetricSource.LambdaNode;
        private readonly string _functionName;
        private readonly string _logGroup;

        public ViewLogStreamsCommand(string functionName, ToolkitContext toolkitContext,
            AwsConnectionSettings connectionSettings) : base(toolkitContext, connectionSettings)
        {
            _functionName = functionName;
            _logGroup = CreateLogGroupName(functionName);
        }

        public override ActionResults Execute()
        {
            ActionResults result = ViewLogStreams();
            CloudWatchTelemetry.RecordOpenLogGroup(result.Success, ViewLogsMetricSource,
                ConnectionSettings, _toolkitContext);
            return result;
        }


        private ActionResults ViewLogStreams()
        {
            try
            {
                CreateLogStreamsViewer();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error viewing CloudWatch log streams for Lambda: {_functionName}", ex);
                _toolkitContext.ToolkitHost.OutputToHostConsole($"Unable to view CloudWatch log streams for Lambda {_functionName}: {ex.Message}",
                    true);
                return new ActionResults().WithSuccess(false);
            }

            return new ActionResults().WithSuccess(true);
        }

        private void CreateLogStreamsViewer()
        {
            var logGroupViewer =
                _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)) as
                    ILogStreamsViewer;
            if (logGroupViewer == null)
            {
                throw new Exception("Unable to load CloudWatch log group data source");
            }

            logGroupViewer.View(_logGroup, ConnectionSettings);
        }

        private string CreateLogGroupName(string functionName)
        {
            return $"/aws/lambda/{functionName}";
        }
    }
}
