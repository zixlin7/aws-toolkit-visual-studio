using System;
using System.Diagnostics;
using System.IO;

using Amazon.AWSToolkit.Shared;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    internal static class JsonRpcExtensionMethods
    {
        private static readonly string _lspTraceFolder = Path.Combine(Path.GetTempPath(), "AwsToolkit", "Lsp");

#if DEBUG
        /// <summary>
        /// All messages sent on the given Json Rpc channel will be written to a file.
        /// This is intended for use when developing the Toolkit, when we need to check
        /// that the messages being transmitted match our expectations.
        /// </summary>
        /// <remarks>Do not turn this on in release builds - we do not want the contents of the LSP session stored</remarks>
        internal static void LogJsonRpcMessages(this JsonRpc rpc, object sender, IAWSToolkitShellProvider toolkitHost)
        {
            try
            {
                var listener = CreateTraceListener(sender, toolkitHost);

                rpc.TraceSource.Listeners.Add(listener);
                rpc.TraceSource.Switch.Level |= SourceLevels.Verbose;

                Trace.AutoFlush = true;
            }
            catch (Exception e)
            {
                toolkitHost.OutputToHostConsole($"Failed to set up LSP logging: {e.Message}", true);
            }
        }

        private static TraceListener CreateTraceListener(object sender, IAWSToolkitShellProvider toolkitHost)
        {
            var logFilePath = Path.Combine(_lspTraceFolder, $"{sender.GetType().Name}.svclog");
            toolkitHost.OutputToHostConsole($"CodeWhisperer LSP messages are being logged to: {logFilePath}", true);
            Directory.CreateDirectory(_lspTraceFolder);
            return new XmlWriterTraceListener(logFilePath);
        }
#endif
    }
}
