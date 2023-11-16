using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.VsSdk.Common.Documents;
using Amazon.AwsToolkit.VsSdk.Common.Services;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

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
        private OleMenuCommandService _ole;
        private readonly ISuggestionUiManager _suggestionUiManager;

        [ImportingConstructor]
        public GetSuggestionsMenuCommand(
            ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _manager = manager;
            _suggestionUiManager = suggestionUiManager;
            _toolkitContextProvider = toolkitContextProvider;
            _taskFactoryProvider = taskFactoryProvider;
        }

        public async Task RegisterAsync(IAsyncServiceProvider service)
        {
            await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

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

        protected async Task ExecuteAsync()
        {
            if (!CanExecute())
            {
                return;
            }

            var toolkitHost = _toolkitContextProvider.GetToolkitContext().ToolkitHost;

            // TODO : IDE-11363 : using a task notifier is temporary. It helps during development to know that a query is running.
            var taskNotifier = await toolkitHost.CreateTaskStatusNotifier();
            taskNotifier.Title = "Retrieving CodeWhisperer suggestions";
            taskNotifier.CanCancel = false;
            taskNotifier.ShowTaskStatus(async _ =>
            {
                var textView = GetTextView();
                var codeWhispererTextView = new CodeWhispererTextView(textView);
                var requestPosition = codeWhispererTextView.GetWpfTextView().GetCaretSnapshotPosition();
                var request = codeWhispererTextView.CreateGetSuggestionsRequest(false);

                var suggestionSession = await _manager.GetSuggestionsAsync(request);

                // only if suggestion session is valid, attempt to load suggestions in UI
                if (suggestionSession.IsValid())
                {
                    var invocationProperties =
                        SuggestionUtilities.CreateInvocationProperties(request.IsAutoSuggestion, suggestionSession.SessionId, requestPosition, suggestionSession.RequestedAtEpoch);
                    await _suggestionUiManager.ShowAsync(suggestionSession.Suggestions, invocationProperties, codeWhispererTextView);
                }
            });
        }

        private bool CanExecute()
        {
            return _toolkitContextProvider.HasToolkitContext() &&
                   _manager.ClientStatus == LspClientStatus.Running &&
                   _manager.ConnectionStatus == ConnectionStatus.Connected;
        }

        private IWpfTextView GetTextView()
        {
            _textManager.GetActiveView(1, null, out var vsTextView);

            return vsTextView.GetWpfTextView();
        }
    }
}
