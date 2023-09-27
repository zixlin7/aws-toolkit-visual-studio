using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.VsSdk.Common.Services;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Models.Text;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using Microsoft;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    [Export(typeof(IPluginCommand))]
    public class GetSuggestionsMenuCommand : IPluginCommand
    {
        public const string CommandSetGuidString = "8ba6f49c-ca32-4bc4-a71c-77b8503b93c2";
        public static readonly Guid CommandSetGuid = new Guid(CommandSetGuidString);
        public const uint CommandId = 0x0700;

        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly ICodeWhispererManager _manager;
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private IVsTextManager _textManager;
        private DTE2 _dte;
        private OleMenuCommandService _ole;

        [ImportingConstructor]
        public GetSuggestionsMenuCommand(
            ICodeWhispererManager manager,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _manager = manager;
            _toolkitContextProvider = toolkitContextProvider;
            _taskFactoryProvider = taskFactoryProvider;
        }

        public async Task RegisterAsync(IAsyncServiceProvider service)
        {
            await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

            _dte = await service.GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(_dte);

            _textManager = await service.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            Assumes.Present(_textManager);

            _ole = await service.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Assumes.Present(_ole);

            await CreateMenuCommandAsync();
        }

        public async Task CreateMenuCommandAsync()
        {
            await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandId = new CommandID(CommandSetGuid, (int) CommandId);

            var command = new OleMenuCommand(ExecuteMenuCommand, commandId);

            _ole?.AddCommand(command);
        }

        protected void ExecuteMenuCommand(object sender, EventArgs args)
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () => await ExecuteAsync());
        }

        // TODO: Converge duplicated logic here and GetSuggestionCommand
        protected async Task ExecuteAsync()
        {
            var toolkitHost = _toolkitContextProvider.GetToolkitContext().ToolkitHost;

            // TODO : IDE-11363 : using a task notifier is temporary. It helps during development to know that a query is running.
            var taskNotifier = await toolkitHost.CreateTaskStatusNotifier();
            taskNotifier.Title = "Retrieving CodeWhisperer suggestions";
            taskNotifier.CanCancel = false;
            taskNotifier.ShowTaskStatus(async _ =>
            {
                var request = await CreateGetSuggestionsRequestAsync();
                var suggestions = (await _manager.GetSuggestionsAsync(request)).ToList();

                // TODO : IDE-11521 : display suggestions to user
                // Until then, output the suggestions in the Output pane

                toolkitHost.OutputToHostConsole($"---------- Suggestions: {suggestions.Count} ----------");

                foreach (var suggestion in suggestions)
                {
                    toolkitHost.OutputToHostConsole(suggestion.Text);
                    toolkitHost.OutputToHostConsole("------------------------------");
                }
            });
        }

        private async Task<GetSuggestionsRequest> CreateGetSuggestionsRequestAsync()
        {
            var cursorPosition = GetCursorPosition();
            var filePath = await GetFilePathAsync();

            return new GetSuggestionsRequest()
            {
                FilePath = filePath,
                CursorPosition = cursorPosition,
                IsAutoSuggestion = false,
            };
        }

        private Position GetCursorPosition()
        {
            _textManager.GetActiveView(1, null, out var activeView);

            // 0-indexed caret position. This is where the suggestion should appear, but the request should be sourced with virtual spaces removed
            activeView.GetCaretPos(out var line, out var column);

            activeView.GetNearestPosition(line, column, out _, out var virtualSpaces);

            var columnStartPosition = column - virtualSpaces;

            return new Position(line, columnStartPosition);
        }

        private async Task<string> GetFilePathAsync()
        {
            await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

            return _dte.ActiveDocument.FullName;
        }
    }
}
