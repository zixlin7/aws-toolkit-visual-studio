using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;

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

        IToolkitLspCredentials CreateToolkitLspCredentials();
    }
}
