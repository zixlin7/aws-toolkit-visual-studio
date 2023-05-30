using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Urls;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Command to view the toolkit user guide and record related metric
    /// </summary>
    public class OpenUserGuideCommand
    {
        public static ICommand Create(ToolkitContext toolkitContext)
        {
            void Record(ITelemetryLogger _)
            {
                toolkitContext.TelemetryLogger.RecordAwsHelp(new AwsHelp()
                {
                    AwsAccount = MetadataValue.NotApplicable,
                    AwsRegion = MetadataValue.NotApplicable,
                });
            }

            return OpenUrlCommandFactory.Create(toolkitContext, AwsUrls.UserGuide, Record);
        }
    }
}
