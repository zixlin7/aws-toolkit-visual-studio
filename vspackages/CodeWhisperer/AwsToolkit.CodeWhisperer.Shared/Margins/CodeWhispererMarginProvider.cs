using System.ComponentModel.Composition;

using Amazon.AWSToolkit.Context;

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
        private readonly IToolkitContextProvider _toolkitContextProvider;

        [ImportingConstructor]
        public CodeWhispererMarginProvider([Import] IToolkitContextProvider toolkitContextProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(
                typeof(CodeWhispererMargin),
                () => new CodeWhispererMargin(_toolkitContextProvider));
        }
    }
}
