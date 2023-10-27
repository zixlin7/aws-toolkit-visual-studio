﻿using System.Windows;
using System.Windows.Controls;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

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

        private readonly IToolkitContextProvider _toolkitContextProvider;
        private readonly UserControl _control;
        private readonly CodeWhispererMarginViewModel _viewModel;

        public CodeWhispererMargin(
            ICodeWhispererTextView textView,
            ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager,
            SVsServiceProvider serviceProvider,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;
            _viewModel = new CodeWhispererMarginViewModel(textView, manager, suggestionUiManager, serviceProvider, _toolkitContextProvider, taskFactoryProvider);

            _control = new CodeWhispererMarginControl()
            {
                DataContext = _viewModel,
            };
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
            _viewModel.Dispose();
        }
    }
}
