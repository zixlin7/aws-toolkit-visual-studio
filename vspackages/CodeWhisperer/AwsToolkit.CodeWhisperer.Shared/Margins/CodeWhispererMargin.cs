using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.VsSdk.Common.Commands;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    /// <summary>
    /// The CodeWhisperer component placed into the Text document margin.
    /// </summary>
    internal class CodeWhispererMargin : IWpfTextViewMargin
    {
        public const string EditorPartName = "CodeWhispererMargin";

        private readonly UserControl _control;
        private readonly CodeWhispererMarginViewModel _marginViewModel;
        private readonly CodeWhispererDocumentViewModel _documentViewModel;
        private readonly IToolkitContextProvider _toolkitContextProvider;

        public CodeWhispererMargin(
            ICodeWhispererTextView textView,
            ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager,
            ICodeWhispererSettingsRepository settingsRepository,
            SVsServiceProvider serviceProvider,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;

            var commandRepository = new ToolkitVsCommandRepository(serviceProvider, taskFactoryProvider);
            _marginViewModel = new CodeWhispererMarginViewModel(textView,
                manager, suggestionUiManager,
                settingsRepository, commandRepository,
                _toolkitContextProvider, taskFactoryProvider);

            _documentViewModel = new CodeWhispererDocumentViewModel(textView, manager, suggestionUiManager);

            _control = new CodeWhispererMarginControl
            {
                DataContext = _marginViewModel,
            };
        }

        public async Task InitializeAsync()
        {
            await _marginViewModel.InitializeAsync();
        }

        public FrameworkElement VisualElement => _control;

        public double MarginSize => _control.DesiredSize.Width;

        public bool Enabled => true;

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return marginName == EditorPartName ? this : (ITextViewMargin) null;
        }

        public void Dispose()
        {
            _marginViewModel.Dispose();
            _documentViewModel.Dispose();
        }
    }
}
