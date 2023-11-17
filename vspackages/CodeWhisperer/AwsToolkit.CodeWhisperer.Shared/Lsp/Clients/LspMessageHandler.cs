using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry;

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
        public async Task<object[]> OnWorkspaceConfigurationAsync(JToken configurationParams)
        {
            try
            {
                var eventArgs = new WorkspaceConfigurationEventArgs() { Request = configurationParams.ToObject<ConfigurationParams>(), };

                await RaiseWorkspaceConfigurationAsync(eventArgs);

                return eventArgs.Response == null ? null : new object[] { eventArgs.Response };
            }
            catch (Exception e)
            {
                _logger.Error("Failed to handle configuration request from language server. Server will be sent a null response.", e);
                return null;
            }
        }

        /// <summary>
        /// Signals that the language server has sent telemetry event notifications
        /// Handler should transform and emit the  <see cref="MetricEvent"/>
        /// to the telemetry backend
        /// </summary>
        
        internal event EventHandler<TelemetryEventArgs> TelemetryEvent;

        /// <summary>
        /// Invoked when notification "telemetry/event" is sent by the language server
        /// </summary>
        [JsonRpcMethod(TelemetryMessageNames.TelemetryNotification)]
        public Task OnTelemetryEventAsync(JToken eventParams)
        {
            try
            {
                var metricEvent = eventParams.ToObject<MetricEvent>();
                var eventArgs = new TelemetryEventArgs() { MetricEvent = metricEvent };
                RaiseTelemetryEvent(eventArgs);
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                // swallow errors that may include error parsing the telemetry event notification from language server
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Signals that the language server is requesting auth connection information
        /// Handler should populate <see cref="ConnectionMetadataEventArgs.Response"/>
        /// with the data to send back to the language server.
        /// </summary>
        internal event AsyncEventHandler<ConnectionMetadataEventArgs> ConnectionMetadataAsync;

        /// <summary>
        /// Invoked when message "$/aws/credentials/getConnectionMetadata" is sent by the language server
        /// </summary>
        [JsonRpcMethod(ConnectionMessageNames.ConnectionMetadataRequested)]
        public async Task<object> OnConnectionMetadataRequestAsync()
        {
            try
            {
                var eventArgs = new ConnectionMetadataEventArgs();
                await RaiseConnectionMetadataRequestAsync(eventArgs);
                return eventArgs.Response;
            }
            catch (Exception e)
            {
                _logger.Error("Failed to handle connection metadata request from language server. Server will be sent a null response.", e);
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

        private void RaiseTelemetryEvent(TelemetryEventArgs eventArgs)
        {
            var telemetryEvent = TelemetryEvent;
            if (telemetryEvent != null && eventArgs != null)
            {
                telemetryEvent.Invoke(this, eventArgs);
            }
        }

        private async Task RaiseConnectionMetadataRequestAsync(ConnectionMetadataEventArgs eventArgs)
        {
            var connectionMetadataAsync = ConnectionMetadataAsync;
            if (connectionMetadataAsync != null)
            {
                await connectionMetadataAsync.InvokeAsync(this, eventArgs);
            }
        }
    }
}
