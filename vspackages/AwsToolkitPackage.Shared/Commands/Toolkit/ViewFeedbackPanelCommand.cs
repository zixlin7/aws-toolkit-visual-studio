using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.VisualStudio.Shell;
using Amazon.AWSToolkit.Feedback;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for opening the toolkit feedback form
    /// </summary>
    public class ViewFeedbackPanelCommand : BaseCommand<ViewFeedbackPanelCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ViewFeedbackPanelCommand));

        private readonly ToolkitContext _toolkitContext;
        private readonly ICommand _sendFeedbackCommand;

        public ViewFeedbackPanelCommand(ToolkitContext toolkitContext, SendFeedbackCommand command)
        {
            _toolkitContext = toolkitContext;
            _sendFeedbackCommand = command;
        }

        public static Task<ViewFeedbackPanelCommand> InitializeAsync(
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            var sendFeedbackCommand = new SendFeedbackCommand(toolkitContext);
            return InitializeAsync(
                () => new ViewFeedbackPanelCommand(toolkitContext, sendFeedbackCommand),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs args)
        {
            try
            {
                _sendFeedbackCommand.Execute(null);
            }
            catch (Exception e)
            {
                Logger.Error($"Error launching feedback form", e);
                _toolkitContext.ToolkitHost.ShowError("Failed to open the feedback form", e.Message);
            }
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs e)
        {
            try
            {
                menuCommand.Visible = true;
                _sendFeedbackCommand.CanExecute(null);
            }
            catch
            {
                // Swallow error for stability -- menu will not be visible
                // do not log - this is invoked each time the menu is opened
            }
        }
    }
}

