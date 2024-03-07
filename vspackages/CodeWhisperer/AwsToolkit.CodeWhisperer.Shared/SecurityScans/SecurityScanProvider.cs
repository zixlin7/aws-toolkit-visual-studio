using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;
using Amazon.AWSToolkit.Context;
using Community.VisualStudio.Toolkit;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.SecurityScans;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScan
{
    [Export(typeof(ISecurityScanProvider))]
    internal class SecurityScanProvider : ISecurityScanProvider
    {
        private readonly ICodeWhispererLspClient _lspClient;
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private SecurityScanState _scanState = SecurityScanState.NotRunning;
        public SecurityScanState ScanState
        {
            get => _scanState;
            set
            {
                if (_scanState != value)
                {
                    _scanState = value;
                    RaiseStatusChanged(value);
                }
            }
        }

        [ImportingConstructor]
        public SecurityScanProvider(
            ICodeWhispererLspClient lspClient,
            IToolkitContextProvider toolkitContextProvider)
        {
            _lspClient = lspClient;
            _toolkitContextProvider = toolkitContextProvider;
        }

        public event EventHandler<SecurityScanStateChangedEventArgs> SecurityScanStateChanged;

        public async Task ScanAsync()
        {
            ScanState = SecurityScanState.Running;
            var taskStatus = await _toolkitContextProvider.GetToolkitContext().ToolkitHost.CreateTaskStatusNotifier();
            taskStatus.Title = "Scanning active file and its dependencies...";
            taskStatus.CanCancel = false;
            var securityScan = _lspClient.CreateSecurityScan();

            taskStatus.ShowTaskStatus(async _ =>
            {
                {
                    // TODO: remove the placeholder delay
                    await Task.Delay(5000);

                    var activeDocument = await VS.Documents.GetActiveDocumentViewAsync();
                    var currentFilePath = activeDocument.FilePath;
                    var workspace = await VS.Solutions.GetCurrentSolutionAsync();
                    var json = GetSecurityScanParamArguments(currentFilePath, workspace.FullPath);
                    var request = new ExecuteCommandParams
                    {
                        Command = ExecuteCommandNames.RunSecurityScan,
                        Arguments = new string[] { json }
                    };
                    var response = await securityScan.RunSecurityScanAsync(request);
                    ScanState = SecurityScanState.NotRunning;
                }
            });

        }

        public async Task CancelScanAsync()
        {
            ScanState = SecurityScanState.Cancelling;
            var securityScan = _lspClient.CreateSecurityScan();
            await securityScan.CancelSecurityScanAsync();
            ScanState = SecurityScanState.NotRunning;
        }

        private void RaiseStatusChanged(SecurityScanState securityScanState)
        {
            SecurityScanStateChanged?.Invoke(this, new SecurityScanStateChangedEventArgs(securityScanState));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        private string GetSecurityScanParamArguments(string filePath, string projectPath)
        {
            var options = new JsonWriterOptions
            {
                Indented = true
            };

            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream, options);

            writer.WriteStartObject();
            writer.WriteString("activeFilePath", filePath);
            writer.WriteString("projectPath", projectPath);
            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
