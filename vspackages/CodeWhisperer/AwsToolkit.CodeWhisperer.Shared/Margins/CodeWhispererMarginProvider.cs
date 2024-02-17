using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Mef;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    /// <summary>
    /// Provides the CodeWhisperer margin for Visual Studio.
    /// Activated by Visual Studio.
    /// 
    /// References:
    /// https://learn.microsoft.com/en-us/visualstudio/extensibility/inside-the-editor?view=vs-2022#margins
    /// https://learn.microsoft.com/en-us/visualstudio/extensibility/language-service-and-editor-extension-points?view=vs-2022#extend-margins-and-scrollbars
    /// </summary>
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(CodeWhispererMargin.EditorPartName)]
    [Order(After = PredefinedMarginNames.ZoomControl, Before = PredefinedMarginNames.FileHealthIndicator)]
    [MarginContainer(PredefinedMarginNames.BottomControl)]
    [ContentType(StandardContentTypeNames.Code)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class CodeWhispererMarginProvider : IWpfTextViewMarginProvider
    {
        private readonly ICodeWhispererManager _manager;
        private readonly ISuggestionUiManager _suggestionUiManager;
        private readonly ICodeWhispererSettingsRepository _settingsRepository;
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;

        [ImportingConstructor]
        public CodeWhispererMarginProvider(
            ICodeWhispererManager manager,
            // CentralComponents - Auto-instantiate CodeWhisperer MEF components that aren't related
            // to the VS SDK (and aren't otherwise auto-created by Visual Studio).
            CentralComponents centralComponents,
            ISuggestionUiManager suggestionUiManager,
            ICodeWhispererSettingsRepository settingsRepository,
            SVsServiceProvider serviceProvider,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _manager = manager;
            _suggestionUiManager = suggestionUiManager;
            _settingsRepository = settingsRepository;
            _serviceProvider = serviceProvider;
            _toolkitContextProvider = toolkitContextProvider;
            _taskFactoryProvider = taskFactoryProvider;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(
                typeof(CodeWhispererMargin),
                () => _taskFactoryProvider.JoinableTaskFactory.Run(
                    async () => await CreateCodeWhispererMarginAsync(wpfTextViewHost)));
        }

        private async Task<CodeWhispererMargin> CreateCodeWhispererMarginAsync(IWpfTextViewHost wpfTextViewHost)
        {
            var margin = new CodeWhispererMargin(
                new CodeWhispererTextView(wpfTextViewHost.TextView),
                _manager, _suggestionUiManager,
                _settingsRepository,
                _serviceProvider,
                _toolkitContextProvider, _taskFactoryProvider);

            await margin.InitializeAsync();

            return margin;
        }
    }
}
