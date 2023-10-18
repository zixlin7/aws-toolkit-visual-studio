using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration
{
    /// <summary>
    /// Used with events signalling that a language server has requested
    /// the current workspace/configuration state.
    /// </summary>
    public class WorkspaceConfigurationEventArgs : EventArgs
    {
        /// <summary>
        /// The request payload sent from the language server
        /// </summary>
        public ConfigurationParams Request { get; set; }

        /// <summary>
        /// The handler sets this to the object that will be sent back to
        /// the language server as a response to its request.
        /// </summary>
        public Dictionary<string, object> Response { get; } = new Dictionary<string, object>();
    }
}
