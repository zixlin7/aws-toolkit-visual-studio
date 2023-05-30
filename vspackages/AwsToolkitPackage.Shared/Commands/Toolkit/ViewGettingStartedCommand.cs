using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Controller;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for opening the AWS Toolkit Getting Started dialog
    /// </summary>
    public class ViewGettingStartedCommand : BaseCommand<ViewGettingStartedCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ViewGettingStartedCommand));

        private readonly AWSToolkitPackage _toolkitPackage;
        private readonly ToolkitContext _toolkitContext;
        private readonly ToolkitSettingsWatcher _toolkitSettingsWatcher;

        public ViewGettingStartedCommand(AWSToolkitPackage toolkitPackage, ToolkitContext toolkitContext, ToolkitSettingsWatcher toolkitSettingsWatcher)
        {
            _toolkitPackage = toolkitPackage;
            _toolkitContext = toolkitContext;
            _toolkitSettingsWatcher = toolkitSettingsWatcher;
        }

        public static Task<ViewGettingStartedCommand> InitializeAsync(
            AWSToolkitPackage toolkitPackage,
            ToolkitContext toolkitContext,
            ToolkitSettingsWatcher toolkitSettingsWatcher,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new ViewGettingStartedCommand(toolkitPackage, toolkitContext, toolkitSettingsWatcher),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs args)
        {
            try
            {
                var controller = new LegacyFirstRunController(_toolkitPackage, _toolkitSettingsWatcher, _toolkitContext);
                controller.Execute();
            }
            catch (Exception e)
            {
                Logger.Error($"Error showing Getting Started dialog", e);
                _toolkitContext.ToolkitHost.ShowError("Failed to open the Getting Started screen", e.Message);
            }
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs e)
        {
            try
            {
                menuCommand.Visible = true;
            }
            catch
            {
                // Swallow error for stability -- menu will not be visible
                // do not log - this is invoked each time the menu is opened
            }
        }
    }
}
