using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AWSToolkit.Context;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public class LspInstaller : ILspInstaller
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LspInstaller));
        private readonly ICodeWhispererSettingsRepository _codeWhispererSettingsRepository;
        private readonly ToolkitContext _toolkitContext;

        public LspInstaller(ToolkitContext toolkitContext,
            ICodeWhispererSettingsRepository codeWhispererSettingsRepository)
        {
            _toolkitContext = toolkitContext;
            _codeWhispererSettingsRepository = codeWhispererSettingsRepository;
        }

        public async Task ExecuteAsync(CancellationToken token = default)
        {
            try
            {
                var manifestSchema = await VersionManifestManager.Create(_codeWhispererSettingsRepository)
                    .DownloadAsync(token);
                if (manifestSchema == null)
                {
                    throw new Exception("Error retrieving lsp version manifest");
                }

                var lspManager = new LspManager(_codeWhispererSettingsRepository, manifestSchema, _toolkitContext);
                var downloadPath = await lspManager.DownloadAsync(token);

                // TODO: Return download Path for installation process
                // TODO: Handle cancellations for long loading for install operation
            }
            catch (Exception ex)
            {
                _logger.Error("Error installing the Language server", ex);
            }
        }
    }
}
