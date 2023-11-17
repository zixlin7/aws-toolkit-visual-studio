using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.Notifications;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Notification
{
    public class ManifestDeprecationInfoBarManager : ToolkitInfoBarManager
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ManifestDeprecationInfoBarManager));
        private readonly ManifestDeprecationStrategy _manifestStrategy;
        private readonly VersionManifestOptions _options;

        public ManifestDeprecationInfoBarManager(ManifestDeprecationStrategy manifestStrategy,
            IServiceProvider serviceProvider, ToolkitContext toolkitContext) : base(serviceProvider,
            toolkitContext)
        {
            _options = manifestStrategy.Options;
            _manifestStrategy = manifestStrategy;
        }

        /// <summary>
        /// Displays an info bar relaying the deprecation notice for language server version being referenced with this toolkit
        /// </summary>
        public async Task ShowInfoBarAsync(ManifestSchema schema, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var result = await _manifestStrategy.CanShowNotificationAsync(schema, token);
                if (result)
                {
                    ShowInfoBar();
                }
            }
            catch (Exception e)
            {
                _logger.Error(
                    $"Error while displaying version manifest deprecation notice for {_options.Name} language server",
                    e);
            }
        }

        protected override string _identifier => $"version manifest deprecation of {_options.Name} language server";

        protected override ToolkitInfoBar CreateInfoBar()
        {
            return new ManifestDeprecationInfoBar(_manifestStrategy);
        }
    }
}
