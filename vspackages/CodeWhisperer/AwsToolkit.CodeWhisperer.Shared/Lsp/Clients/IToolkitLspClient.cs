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
        /// Requests a JSON-PRC Proxy that is used to access set of
        /// notifications/requests on the language server.
        /// See also: https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/index.md
        /// </summary>
        /// <typeparam name="TProxy">The interface proxy to request</typeparam>
        TProxy CreateProxy<TProxy>() where TProxy : class;
    }
}
