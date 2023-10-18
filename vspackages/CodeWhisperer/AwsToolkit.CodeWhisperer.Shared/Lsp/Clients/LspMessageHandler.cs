using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;

using log4net;

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json.Linq;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    /// <summary>
    /// Provides Visual Studio with the handlers for custom messages emitted by the language server.
    ///
    /// This class isn't consumed by the Toolkit. Visual Studio uses this as part of its language client
    /// orchestration.
    ///
    /// See: https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=vs-2022#receive-custom-messages
    /// 
    /// This class defines the base handlers applicable to all language servers.
    /// If a certain language server has specific messages the Toolkit needs to respond to,
    /// a derived class should be created that contains the service-specific support.
    /// The derived class should be provided to the language client using an overloaded
    /// <see cref="ToolkitLspClient.CreateLspMessageHandler"/> implementation.
    /// </summary>
    public class LspMessageHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LspMessageHandler));

        /// <summary>
        /// Signals that the language server is requesting configuration state.
        /// Handler should populate <see cref="WorkspaceConfigurationEventArgs.Response"/>
        /// with the data to send back to the language server.
        /// </summary>
        internal event AsyncEventHandler<WorkspaceConfigurationEventArgs> WorkspaceConfigurationAsync;

        /// <summary>
        /// Invoked when message "workspace/configuration" is sent by the language server
        /// </summary>
        [JsonRpcMethod(MessageNames.ConfigurationRequested)]
        public async Task<object> OnWorkspaceConfigurationAsync(JToken configurationParams)
        {
            try
            {
                var eventArgs = new WorkspaceConfigurationEventArgs() { Request = configurationParams.ToObject<ConfigurationParams>(), };

                await RaiseWorkspaceConfigurationAsync(eventArgs);
                return eventArgs.Response;
            }
            catch (Exception e)
            {
                _logger.Error("Failed to handle configuration request from language server. Server will be sent a null response.", e);
                return null;
            }
        }

        private async Task RaiseWorkspaceConfigurationAsync(WorkspaceConfigurationEventArgs eventArgs)
        {
            var workspaceConfigurationAsync = WorkspaceConfigurationAsync;
            if (workspaceConfigurationAsync != null && eventArgs.Request != null)
            {
                await workspaceConfigurationAsync.InvokeAsync(this, eventArgs);
            }
        }
    }
}
