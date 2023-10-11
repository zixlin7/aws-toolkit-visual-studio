using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;

using AwsToolkit.VsSdk.Common.Settings;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Installs the CodeWhisperer LSP
    /// </summary>
    public class CodeWhispererInstaller : ILspInstaller
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeWhispererInstaller));
        private readonly ILspSettingsRepository _lspSettingsRepository;
        private readonly ToolkitContext _toolkitContext;

        public CodeWhispererInstaller(ToolkitContext toolkitContext,
            ILspSettingsRepository lspSettingsRepository)
        {
            _toolkitContext = toolkitContext;
            _lspSettingsRepository = lspSettingsRepository;
        }

        public async Task<string> ExecuteAsync(ITaskStatusNotifier notifier)
        {
            var statusMsg = "Successfully installed CodeWhisperer Language Server";
            try
            {
                notifier.CancellationToken.ThrowIfCancellationRequested();

                var versionManifestManager = CreateVersionManifestManager();
                var manifestSchema = await versionManifestManager.DownloadAsync(notifier.CancellationToken);

                if (manifestSchema == null)
                {
                    throw new ToolkitException("Error retrieving CodeWhisperer Language Server version manifest",
                        ToolkitException.CommonErrorCode.UnsupportedState);
                }

                var lspManager = CreateLspManager(manifestSchema);
                return await lspManager.DownloadAsync(notifier.CancellationToken);
            }
            catch (Exception ex)
            {
                statusMsg = "Error installing CodeWhisperer Language Server";
                var notifierMsg =
                    "Error installing CodeWhisperer Language Server. CodeWhisperer functionality is unavailable. See AWS Toolkit logs for details.";

                if (ex is OperationCanceledException)
                {
                    notifierMsg =
                        "CodeWhisperer Language Server install canceled. CodeWhisperer functionality is unavailable";
                    statusMsg = "CodeWhisperer Language Server install canceled";
                }

                // show install status
                notifier.ProgressText = notifierMsg;
                _toolkitContext.ToolkitHost.OutputToHostConsole(notifierMsg, true);
                _logger.Error(statusMsg, ex);

                throw GetException(statusMsg, ex);
            }
            finally
            {
                _toolkitContext.ToolkitHost.UpdateStatus(statusMsg);
            }
        }

        /// <summary>
        /// If operation is cancelled, return exception as is, else wrap it as toolkit exception for appropriate indication with the task notifier
        /// </summary>
        private Exception GetException(string message, Exception exception)
        {
            return exception is OperationCanceledException
                ? exception
                : new ToolkitException(message, ToolkitException.CommonErrorCode.UnexpectedError, exception);
        }

        private VersionManifestManager CreateVersionManifestManager()
        {
            var options = new VersionManifestManager.Options()
            {
                Name = "CodeWhisperer",
                FileName = CodeWhispererConstants.ManifestFilename,
                MajorVersion = CodeWhispererConstants.ManifestCompatibleMajorVersion
            };
            return VersionManifestManager.Create(options, _lspSettingsRepository);
        }

        private LspManager CreateLspManager(ManifestSchema manifestSchema)
        {
            var options = new LspManager.Options()
            {
                Name = "CodeWhisperer",
                Filename = CodeWhispererConstants.Filename,
                ToolkitContext = _toolkitContext,
                VersionRange = CodeWhispererConstants.LspCompatibleVersionRange,
                DownloadParentFolder = CodeWhispererConstants.LspDownloadParentFolder
            };
            return new LspManager(options, _lspSettingsRepository, manifestSchema);
        }
    }
}
