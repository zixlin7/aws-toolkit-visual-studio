using System;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    /// <summary>
    /// General encapsulation for a language client.
    /// Other MEF components should access the language server through this interface.
    /// </summary>
    public interface IToolkitLspClient
    {
        // TODO : additional methods we'll want other components to access
        // bool IsRunning { get; }
        // Start()
        // Stop()

        /// <summary>
        /// Raised when the language client has successfully completed the initialization handshake with the language server.
        /// </summary>
        event AsyncEventHandler<EventArgs> InitializedAsync;

        /// <summary>
        /// Raised when the language server requests the current workspace configuration.
        /// </summary>
        event AsyncEventHandler<WorkspaceConfigurationEventArgs> RequestWorkspaceConfigurationAsync;

        /// <summary>
        /// Produces the abstraction capable of handling the language server's credentials messages 
        /// </summary>
        IToolkitLspCredentials CreateToolkitLspCredentials();

        /// <summary>
        /// Produces the abstraction capable of handling the language server's configuration messages 
        /// </summary>
        ILspConfiguration CreateLspConfiguration();
    }
}
