﻿using System.ComponentModel.Composition;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
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
        private readonly ICodeWhispererManager _manager;
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private readonly ISuggestionUiManager _suggestionUiManager;

        [ImportingConstructor]
        public CodeWhispererMarginProvider(
            ICodeWhispererManager manager,
            // ExpirationNotificationManager is not related to the VSSDK, so it isn't
            // auto-created by Visual Studio. This margin provider is a central
            // CodeWhisperer component that gets activated by Visual Studio, so we
            // import ExpirationNotificationManager here to auto instantiate it.
            IExpirationNotificationManager expirationNotificationManager,
            // CodeWhispererSettingsPublisher is not related to the VSSDK, so it isn't
            // auto-created by Visual Studio. This margin provider is a central
            // CodeWhisperer component that gets activated by Visual Studio, so we
            // import ICodeWhispererSettingsPublisher here to auto instantiate it.
            ICodeWhispererSettingsPublisher codeWhispererSettingsPublisher,
            IToolkitContextProvider toolkitContextProvider,
            ISuggestionUiManager suggestionUiManager)
        {
            _manager = manager;
            _toolkitContextProvider = toolkitContextProvider;
            _suggestionUiManager = suggestionUiManager;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(
                typeof(CodeWhispererMargin),
                () => new CodeWhispererMargin(wpfTextViewHost.TextView, _manager, _suggestionUiManager, _toolkitContextProvider));
        }
    }
}
