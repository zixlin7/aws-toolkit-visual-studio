using System;
using System.IO;
using Amazon.AWSToolkit.Settings;
using Amazon.Runtime.Internal.Settings;
using log4net;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Wrapper around the Toolkit Settings to work with user-configured hosted files settings.
    /// </summary>
    public class HostedFilesSettings
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(HostedFilesSettings));

        private readonly ToolkitSettings _toolkitSettings;
        private readonly string _downloadedCacheParentFolder;

        /// <summary>
        /// Converts the user-configured hosted files value to a Uri, which
        /// could be null (use system defaults), or a Uri that points to
        /// either a location on disk or online.
        /// </summary>
        public Uri HostedFilesLocationAsUri
        {
            get
            {
                var location = _toolkitSettings.HostedFilesLocation;

                if (string.IsNullOrWhiteSpace(location))
                {
                    return null;
                }

                if (location.StartsWith(S3FileFetcher.REGIONALENDPOINTSCHEME, StringComparison.OrdinalIgnoreCase))
                {
                    // eg: "region://us-west-2"
                    return GetRegionalS3Uri(location);
                }

                // eg: "file://<some-file>" , "http://<some-url>"
                return new Uri(location);
            }
        }

        /// <summary>
        /// Location where hosted files contents are stored to when they are downloaded
        /// </summary>
        public string DownloadedCacheFolder => Path.Combine(_downloadedCacheParentFolder, "downloadedfiles");


        public HostedFilesSettings() : this(ToolkitSettings.Instance, PersistenceManager.GetSettingsStoreFolder())
        {

        }

        public HostedFilesSettings(ToolkitSettings toolkitSettings, string downloadedCacheParentFolder)
        {
            _toolkitSettings = toolkitSettings;
            _downloadedCacheParentFolder = downloadedCacheParentFolder;
        }

        /// <summary>
        /// Convert "region://(some-region)" into a url
        /// </summary>
        private static Uri GetRegionalS3Uri(string location)
        {
            if (!location.StartsWith(S3FileFetcher.REGIONALENDPOINTSCHEME, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var region = location.Substring(S3FileFetcher.REGIONALENDPOINTSCHEME.Length);
            try
            {
                var endpoint = RegionEndpoint.GetBySystemName(region).GetEndpointForService("s3");
                return new Uri(string.Format("https://{0}-{1}.{2}/", S3FileFetcher.AWSVSTOOLKIT_BUCKETPREFIX,
                    region, endpoint));
            }
            catch
            {
                Logger.ErrorFormat("Failed to construct regional hosted files endpoint for location {0}",
                    location);

                return null;
            }
        }
    }
}
