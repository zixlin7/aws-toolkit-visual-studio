﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Tasks;

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
        private const int _cleanupDelay = 10000;
        private readonly ILspSettingsRepository _lspSettingsRepository;
        private readonly ToolkitContext _toolkitContext;

        public CodeWhispererInstaller(ToolkitContext toolkitContext,
            ILspSettingsRepository lspSettingsRepository)
        {
            _toolkitContext = toolkitContext;
            _lspSettingsRepository = lspSettingsRepository;
        }

        public async Task<string> ExecuteAsync(ITaskStatusNotifier notifier, CancellationToken token = default)
        {
            var statusMsg = "Successfully installed CodeWhisperer Language Server";
            try
            {
                token.ThrowIfCancellationRequested();

                var localLspPath = await GetLocalLspPathAsync();
                // if language server local override exists, return that location
                if (!string.IsNullOrWhiteSpace(localLspPath))
                {
                    var msg = $"Launching CodeWhisperer Language Server from local override location: {localLspPath}";
                    _logger.Info(msg);
                    _toolkitContext.ToolkitHost.OutputToHostConsole(msg, true);
                    return localLspPath;
                }

                var versionManifestManager = CreateVersionManifestManager();
                var manifestSchema = await versionManifestManager.DownloadAsync(token);

                if (manifestSchema == null)
                {
                    throw new ToolkitException("Error retrieving CodeWhisperer Language Server version manifest",
                        ToolkitException.CommonErrorCode.UnsupportedState);
                }

                var lspManager = CreateLspManager(manifestSchema);
                var result = await lspManager.DownloadAsync(token);

                // start cleanup on a background thread
                InitiateCleanup(lspManager, token);
                return result;
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
        /// Get local language server path if one exists
        /// </summary>
        private async Task<string> GetLocalLspPathAsync()
        {
            var settings = await _lspSettingsRepository.GetLspSettingsAsync();
            return settings.LanguageServerPath;
        }

        /// <summary>
        /// Initiate cleanup for cached versions of language server
        /// </summary>
        /// <param name="lspManager"></param>
        /// <param name="token"></param>
        private void InitiateCleanup(LspManager lspManager, CancellationToken token)
        {
            Task.Run(async () =>
            {
                // start cleanup on a background thread after a delay of 10sec
                await Task.Delay(_cleanupDelay, token);
                await lspManager.CleanupAsync(token);
            }, token).LogExceptionAndForget();
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
            var options = new VersionManifestOptions()
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
            return new LspManager(options, manifestSchema);
        }
    }
}
