using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Urls;
using log4net;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    public class VersionStrategy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(VersionStrategy));

        private readonly Version _productVersion;
        private readonly AsyncLazy<Version> _latestVersion;

        public VersionStrategy(string productVersion) : this(productVersion, FetchLatestVersionAsync)
        {
        }

        public VersionStrategy(string productVersion, Func<Task<Version>> fetchLatestVersionAsync)
        {
            _productVersion = new Version(productVersion);
            _latestVersion = new AsyncLazy<Version>(fetchLatestVersionAsync);
        }

        public async Task<bool> IsVersionWithinDisplayConditionsAsync(Notification notification)
        {
            try
            {
                var displayVersion = new[] { "latest", "current" }.Contains(notification.DisplayIf.ToolkitVersion)
                    ? await _latestVersion.GetValueAsync()
                    : new Version(notification.DisplayIf.ToolkitVersion);

                switch (notification.DisplayIf.Comparison)
                {
                    case ">":
                        return _productVersion > displayVersion;
                    case ">=":
                        return _productVersion >= displayVersion;
                    case "<":
                        return _productVersion < displayVersion;
                    case "<=":
                        return _productVersion <= displayVersion;
                    case "==":
                        return _productVersion == displayVersion;
                    case "!=":
                        return _productVersion != displayVersion;
                    default:
                        throw new NotificationToolkitException("Invalid comparison operator",
                            NotificationToolkitException.NotificationErrorCode.InvalidComparator);
                }
            }
            catch (NotificationToolkitException toolkitException)
            {
                throw toolkitException;
            }
            catch (Exception e)
            {
                throw new NotificationToolkitException("Invalid toolkit version",
                    NotificationToolkitException.NotificationErrorCode.InvalidToolkitVersion, e);
            }
        }

        public static async Task<Version> FetchLatestVersionAsync()
        {
            var versionManifestJson = await FetchVersionManifestAsync();
            return DeserializeVersionManifest(versionManifestJson);
        }

        private static async Task<string> FetchVersionManifestAsync()
        {
            try
            {
                return await NotificationUtilities.FetchHttpContentAsStringAsync(ManifestUrls.VersionManifest);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                throw new NotificationToolkitException("Failed to fetch toolkit version from manifest",
                    NotificationToolkitException.NotificationErrorCode.InvalidFetchVersionRequest, e);
            }
        }

        private static Version DeserializeVersionManifest(string json)
        {
            try
            {
                var versionManifest = JsonConvert.DeserializeObject<VersionManifest>(json);
                return new Version(versionManifest.version);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);
                throw new NotificationToolkitException("Failed to deserialize version manifest",
                    NotificationToolkitException.NotificationErrorCode.InvalidVersionManifest, e);
            }
        }
    }
}
