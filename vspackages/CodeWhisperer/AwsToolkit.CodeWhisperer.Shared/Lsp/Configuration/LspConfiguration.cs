using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration
{
    internal static class MessageNames
    {
        /// <summary>
        /// Pushed by the Toolkit to the language server to signal that
        /// configuration(s) may have changed.
        /// </summary>
        public const string DidChangeConfiguration = "workspace/didChangeConfiguration";

        /// <summary>
        /// Requested by the language server to query the current configuration state
        /// </summary>
        public const string ConfigurationRequested = "workspace/configuration";
    }

    /// <summary>
    /// Abstraction for configuration related communications with the language server
    /// </summary>
    public interface ILspConfiguration
    {
        Task RaiseDidChangeConfigurationAsync(object configuration);
    }

    /// <summary>
    /// Configuration related communications with the language server.
    /// </summary>
    public class LspConfiguration : ILspConfiguration
    {
        private readonly JsonRpc _rpc;

        public LspConfiguration(JsonRpc rpc)
        {
            _rpc = rpc;
        }

        public async Task RaiseDidChangeConfigurationAsync(object configuration)
        {
            var configurationParams = new DidChangeConfigurationParams() { Settings = configuration, };

            await _rpc.NotifyWithParameterObjectAsync(MessageNames.DidChangeConfiguration, configurationParams);
        }
    }
}
