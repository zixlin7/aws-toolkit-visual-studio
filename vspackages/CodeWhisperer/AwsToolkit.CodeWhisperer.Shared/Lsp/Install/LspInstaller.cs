using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Settings;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public class LspInstaller : ILspInstaller
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LspInstaller));
        private readonly ICodeWhispererSettingsRepository _codeWhispererSettingsRepository;

        public LspInstaller(ICodeWhispererSettingsRepository codeWhispererSettingsRepository)
        {
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
                // TODO: Use the schema to download, validate and manage the Language server

                // TODO: Handle cancellations for long loading for install operation
            }
            catch (Exception ex)
            {
                _logger.Error("Error installing the Language server", ex);
            }
        }
    }
}
