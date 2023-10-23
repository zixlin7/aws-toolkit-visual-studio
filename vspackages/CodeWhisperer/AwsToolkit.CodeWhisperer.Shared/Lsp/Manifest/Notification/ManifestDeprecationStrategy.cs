using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.Context;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Notification
{
    public class ManifestDeprecationStrategy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ManifestDeprecationStrategy));

        private readonly ToolkitContext _toolkitContext;

        public VersionManifestOptions Options { get; }

        public ManifestDeprecationStrategy(VersionManifestOptions options, ToolkitContext toolkitContext)
        {
            Options = options;
            _toolkitContext = toolkitContext;
        }

        public Task<bool> CanShowNotificationAsync(ManifestSchema schema, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task MarkNotificationAsDismissedAsync()
        {
            throw new NotImplementedException();

        }

        public Task ShowMarketplaceAsync()
        {
            throw new NotImplementedException();
        }
    }
}
