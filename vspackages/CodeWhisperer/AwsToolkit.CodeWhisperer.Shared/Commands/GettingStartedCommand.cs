using System.ComponentModel.Design;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.VisualStudio.Threading;

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
                // MAGIC!  This command is used by both the margin menu and a hyperlink on the CredentialSelectionDialog.
                // await TaskSchedule.Default is required for the AddEditProfileWizard to load properly when this command
                // is executed from the margin menu.  This is not required when this command is executed from the
                // CredentialSelectionDialog, but it works fine when it is here.  If you remove this, the AddEditProfileWizard
                // will not load on Getting Started when launched from the margin menu.
                await TaskScheduler.Default;
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
