using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Notification;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using AwsToolkit.VsSdk.Common.Settings;

using log4net;

using Microsoft.VisualStudio.Threading;

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
        private readonly IServiceProvider _serviceProvider;

        public CodeWhispererInstaller(IServiceProvider serviceProvider, ILspSettingsRepository lspSettingsRepository, ToolkitContext toolkitContext)
        {
            _serviceProvider = serviceProvider;
            _lspSettingsRepository = lspSettingsRepository;
            _toolkitContext = toolkitContext;
        }

        public async Task<LspInstallResult> ExecuteAsync(ITaskStatusNotifier notifier, CancellationToken token = default)
        {
            var statusMsg = "Successfully installed CodeWhisperer Language Server";
            try
            {
                token.ThrowIfCancellationRequested();

                var localLspResult = await GetLspFromLocalOverrideAsync();
                // if language server local override exists, return that location
                if (!string.IsNullOrWhiteSpace(localLspResult.Path))
                {
                    var msg = $"Launching CodeWhisperer Language Server from local override location: {localLspResult.Path}";
                    _logger.Info(msg);
                    _toolkitContext.ToolkitHost.OutputToHostConsole(msg, true);
                    return localLspResult;
                }

                var versionManifestManager = CreateVersionManifestManager();
                var manifestSchema = await versionManifestManager.DownloadAsync(token);

                if (manifestSchema == null)
                {
                    throw new LspToolkitException("Error retrieving CodeWhisperer Language Server version manifest",
                        LspToolkitException.LspErrorCode.UnexpectedManifestError);
                }

                var lspManager = CreateLspManager(manifestSchema);
                var result = await lspManager.DownloadAsync(token);

                // show deprecation message if schema is deprecated
                ShowDeprecationAsync(manifestSchema, token).LogExceptionAndForget();
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
        /// Get language server from local path if one exists
        /// </summary>
        private async Task<LspInstallResult> GetLspFromLocalOverrideAsync()
        {
            var result = new LspInstallResult
            {
                Path = await GetLocalLspPathAsync(),
                Location = LanguageServerLocation.Override
            };
            return result;
        }

        private async Task<string> GetLocalLspPathAsync()
        {
            try
            {
                var settings = await _lspSettingsRepository.GetLspSettingsAsync();
                return settings.LanguageServerPath;
            }
            catch(Exception ex) when (!(ex is OperationCanceledException))
            {
                // swallow exceptions in this step, so that toolkit can attempt to fetch from remote if this unexpectedly fails
                return null;
            }
        }

        /// <summary>
        /// Shows a deprecation notice if the version manifest has been marked deprecated
        /// </summary>
        private async Task ShowDeprecationAsync(ManifestSchema manifestSchema, CancellationToken token)
        {
            await TaskScheduler.Default;
            var strategy = new ManifestDeprecationStrategy(CreateVersionManifestOptions(true), _lspSettingsRepository, _toolkitContext);
            var manifestDeprecationBarManager = new ManifestDeprecationInfoBarManager(
                strategy, _serviceProvider, _toolkitContext);
            await manifestDeprecationBarManager.ShowInfoBarAsync(manifestSchema, token);
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
            var options = CreateVersionManifestOptions(false);
            return VersionManifestManager.Create(options, _lspSettingsRepository);
        }

        private VersionManifestOptions CreateVersionManifestOptions(bool isFullUrl)
        {
            var options = new VersionManifestOptions()
            {
                Name = "CodeWhisperer",
                FileName = CodeWhispererConstants.ManifestFilename,
                MajorVersion = CodeWhispererConstants.ManifestCompatibleMajorVersion,
                CloudFrontUrl = GetCloudFrontUrl(isFullUrl),
                ToolkitContext = _toolkitContext
            };
            return options;
        }

        private static string GetCloudFrontUrl(bool isFullUrl)
        {
            return isFullUrl ? CodeWhispererConstants.ManifestCloudFrontUrl :CodeWhispererConstants.ManifestBaseCloudFrontUrl;
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
