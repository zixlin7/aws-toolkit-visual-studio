using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.Context;

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

        public async Task<string> ExecuteAsync(CancellationToken token = default)
        {
            try
            {
                var versionManifestManager = CreateVersionManifestManager();
                var manifestSchema = await versionManifestManager.DownloadAsync(token);
                if (manifestSchema == null)
                {
                    throw new Exception("Error retrieving lsp version manifest");
                }

                var lspManager = CreateLspManager(manifestSchema);
                return await lspManager.DownloadAsync(token);

                // TODO: Return download Path for installation process
                // TODO: Handle cancellations for long loading for install operation
            }
            catch (Exception ex)
            {
                _logger.Error("Error installing the Language server", ex);
                return null;
            }
        }

        private VersionManifestManager CreateVersionManifestManager()
        {
            var options = new VersionManifestManager.Options()
            {
                FileName = CodeWhispererConstants.ManifestFilename,
                MajorVersion = CodeWhispererConstants.ManifestCompatibleMajorVersion
            };
            return VersionManifestManager.Create(options, _lspSettingsRepository);
        }

        private LspManager CreateLspManager(ManifestSchema manifestSchema)
        {
            var options = new LspManager.Options()
            {
               Filename = CodeWhispererConstants.Filename,
               ToolkitContext = _toolkitContext,
               VersionRange = CodeWhispererConstants.LspCompatibleVersionRange,
               DownloadParentFolder = CodeWhispererConstants.LspDownloadParentFolder
            };
            return new LspManager(options, _lspSettingsRepository, manifestSchema);
        }
    }
}
