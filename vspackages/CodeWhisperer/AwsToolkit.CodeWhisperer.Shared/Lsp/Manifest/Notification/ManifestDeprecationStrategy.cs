using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Util;

using AwsToolkit.VsSdk.Common.Settings;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Notification
{
    public class ManifestDeprecationStrategy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ManifestDeprecationStrategy));

        private readonly ILspSettingsRepository _settingsRepository;
        private readonly ToolkitContext _toolkitContext;

        public VersionManifestOptions Options { get; }

        public ManifestDeprecationStrategy(VersionManifestOptions options,
            ILspSettingsRepository settingsRepository, ToolkitContext toolkitContext)
        {
            Options = options;
            _settingsRepository = settingsRepository;
            _toolkitContext = toolkitContext;
        }


        /// <remarks>
        /// Note: This works under the assumption that the version manifest local override would never be marked deprecated
        /// </remarks>
        public async Task<bool> CanShowNotificationAsync(ManifestSchema schema, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (!schema.IsManifestDeprecated)
                {
                    return false;
                }

                return !await HasUserDismissedNotificationAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }


        private async Task<bool> HasUserDismissedNotificationAsync()
        {
            try
            {
                var settings = await _settingsRepository.GetLspSettingsAsync();

                var dismissedManifestDeprecation = GetDeprecationForManifest(settings);

                return dismissedManifestDeprecation != null;
            }
            catch (Exception e)
            {
                const string message = "Error while checking if manifest deprecation notification has been dismissed";
                _logger.Error(message, e);
                throw new ToolkitException(message, ToolkitException.CommonErrorCode.UnexpectedError, e);
            }
        }

        public async Task MarkNotificationAsDismissedAsync()
        {
            try
            {
                var settings = await _settingsRepository.GetLspSettingsAsync();

                var dismissedManifestDeprecation = GetDeprecationForManifest(settings);
                // update manifest deprecation notice with appropriate fields
                if (dismissedManifestDeprecation == null)
                {
                    dismissedManifestDeprecation = new DismissedManifestDeprecation()
                    {
                        SchemaMajorVersion = Options.MajorVersion, ManifestUrl = Options.CloudFrontUrl
                    };
                    settings.DismissedManifestDeprecations.Add(dismissedManifestDeprecation);
                    await _settingsRepository.SaveLspSettingsAsync(settings);
                }
            }
            catch (Exception e)
            {
                const string message = "Error dismissing notification for manifest deprecation";
                _logger.Error(message, e);
            }
        }

        public Task ShowMarketplaceAsync()
        {
            NotificationUtil.ShowMarketplace(_toolkitContext);
            return Task.CompletedTask;
        }

        private DismissedManifestDeprecation GetDeprecationForManifest(LspSettings settings)
        {
            return settings?.DismissedManifestDeprecations?
                .FirstOrDefault(x =>
                    x != null && string.Equals(x.ManifestUrl, Options.CloudFrontUrl) &&
                    x.SchemaMajorVersion == Options.MajorVersion);
        }
    }
}
