using System.ComponentModel.Design;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    // TODO Consider renaming to something other than "Getting Started" to not conflate with the
    // general Getting Started classes of the toolkit.
    public class GettingStartedCommand : BaseCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedCommand));

        public GettingStartedCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            const string commandName = "AWSToolkit.GettingStarted";

            var commandId = await _toolkitContextProvider.GetToolkitContext()
                .ToolkitHost.QueryCommandAsync(commandName);

            if (commandId != null)
            {
                await commandId.ExecuteAsync();
            }
            else
            {
                var msg = "Unable to create a profile.  Try Getting Started.";
                _toolkitContextProvider.GetToolkitContext()?.ToolkitHost.ShowError(msg);
                _logger.Warn(msg);
                _logger.Error($"Unable to find {commandName} command.");
            }
        }
    }
}
