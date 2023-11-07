using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class ViewUserGuideCommand : BaseCommand
    {
        public const string UserGuideCwUrl =
            "https://docs.aws.amazon.com/codewhisperer/latest/userguide/getting-started-with-toolkits.html";

        public ViewUserGuideCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            var command = CreateUserGuideCommand();
            command.Execute(null);
            return Task.CompletedTask;
        }

        private ICommand CreateUserGuideCommand()
        {
            var toolkitContext = _toolkitContextProvider.GetToolkitContext();

            void Record(ITelemetryLogger telemetryLogger)
            {
                telemetryLogger.RecordAwsHelp(new AwsHelp()
                {
                    AwsAccount = MetadataValue.NotApplicable,
                    AwsRegion = MetadataValue.NotApplicable,
                });
            }

            return OpenUrlCommandFactory.Create(toolkitContext, UserGuideCwUrl, Record);
        }
    }
}
