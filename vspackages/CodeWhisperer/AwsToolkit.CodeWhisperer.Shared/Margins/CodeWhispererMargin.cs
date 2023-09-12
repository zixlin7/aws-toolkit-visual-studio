using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Context;

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

        public CodeWhispererMargin(IToolkitContextProvider toolkitContextProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;

            _viewModel = new CodeWhispererMarginViewModel(_toolkitContextProvider);

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
        }
    }
}
