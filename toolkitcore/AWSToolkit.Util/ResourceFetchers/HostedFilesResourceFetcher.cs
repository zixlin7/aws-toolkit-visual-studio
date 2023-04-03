using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves hosted files contents based on an order of precedence
    /// that attempts to keep the Toolkit/IDE stable if contents are
    /// unavailable or corrupt.
    /// </summary>
    public class HostedFilesResourceFetcher : IResourceFetcher
    {
        public class Options
        {
            /// <summary>
            /// Whether or not the local download cache is a valid source of hosted files contents.
            /// </summary>
            public bool LoadFromDownloadCache { get; set; } = true;

            /// <summary>
            /// Ensures the Toolkit session downloads the requested contents once (and caches them)
            /// before utilizing the download cache.
            /// This only works for a long lived instance of HostedFilesResourceFetcher.
            /// </summary>
            public bool DownloadOncePerSession { get; set; } = false;

            /// <summary>
            /// Checks if the download cache contents differ from the online source, and
            /// downloads if they differ instead of using the current cache version.
            /// </summary>
            public bool DownloadIfNewer { get; set; } = false;

            /// <summary>
            /// CloudFormation-backed location to fetch resources from
            /// </summary>
            public string CloudFrontBaseUrl { get; set; } = S3FileFetcher.HOSTEDFILES_LOCATION;

            /// <summary>
            /// (Optional) Callback that verifies if the contents can be considered valid,
            /// or if contents should be retrieved from a fallback location.
            /// </summary>
            public Func<Stream, bool> ResourceValidator { get; set; } = null;

            public ITelemetryLogger TelemetryLogger { get; set; }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(HostedFilesResourceFetcher));

        private readonly HostedFilesSettings _hostedFilesSettings;
        private readonly Options _options;
        private readonly HashSet<string> _downloadedResources = new HashSet<string>();

        public HostedFilesResourceFetcher(Options options) : this(options, new HostedFilesSettings())
        {

        }

        public HostedFilesResourceFetcher(Options options, HostedFilesSettings hostedFilesSettings)
        {
            _options = options;
            _hostedFilesSettings = hostedFilesSettings;
        }

        public Stream Get(string relativePath)
        {
            Uri hostedFilesSource = _hostedFilesSettings.HostedFilesLocationAsUri;

            string downloadedCacheFolder = _hostedFilesSettings.DownloadedCacheFolder;

            var downloadCacheFetcher = new RelativeFileResourceFetcher(downloadedCacheFolder);

            var fetcherChain = new ChainedResourceFetcher();

            // If toolkit is configured with a local hosted files location, use that first
            if (hostedFilesSource != null && hostedFilesSource.IsFile)
            {
                fetcherChain.Add(new RelativeFileResourceFetcher(hostedFilesSource.LocalPath));
            }

            // Next, consider using the download cache, if the requesting system allows it
            if (_options.LoadFromDownloadCache)
            {
                fetcherChain.Add(new ConditionalResourceFetcher(downloadCacheFetcher, (stream) =>
                {
                    // For some contents, we want to ensure the Toolkit session downloads it first.
                    // This prevents the scenario where the Toolkit downloads a file and forever 
                    // uses it without downloading further updates.
                    if (_options.DownloadOncePerSession)
                    {
                        // If the file hasn't been downloaded yet, it will be obtained by one of the upcoming fetchers in the chain
                        return _downloadedResources.Contains(relativePath);
                    }

                    // For some contents, check if there is a different version online, and opt to
                    // use that instead of the download cache version.
                    if (_options.DownloadIfNewer && hostedFilesSource != null && !hostedFilesSource.IsFile)
                    {
                        return ContentsMatchCloudFront(stream, relativePath);
                    }

                    return false;
                }));
            }

            // Next, try getting the resource from various online sources.
            // If the resource is obtained this way, it is also cached.
            // Online sources: user-configured, CloudFront, S3
            fetcherChain.Add(CreateHttpBasedFetchers(hostedFilesSource, downloadedCacheFolder));

            // After this point, we're degrading gracefully instead of leaving the Toolkit without any data...

            // Next, unconditionally use contents from the download cache (if available).
            // It might be an old copy of the file, but its better than no copy of the file.
            fetcherChain.Add(downloadCacheFetcher);

            // If the caller determines that the retrieved contents (of fetcherChain) are not valid
            // (or none could be found), attempt to retrieve contents from
            // the Toolkit assembly resources.
            var validatedChainedFetcher = new ConditionalResourceFetcher(fetcherChain, _options.ResourceValidator ?? (stream => true));

            return new ChainedResourceFetcher()
                .Add(validatedChainedFetcher)
                .Add(new AssemblyResourceFetcher())
                .Get(relativePath);
        }

        private IResourceFetcher CreateHttpBasedFetchers(Uri hostedFilesSource, string downloadedCacheFolder)
        {
            string FnGetFullCachePath(string relPath) => Path.Combine(downloadedCacheFolder, relPath);
            var httpFetcherChain = new ChainedResourceFetcher();

            // If the user configured an http based location, use it
            if (hostedFilesSource != null && IsHttpBased(hostedFilesSource))
            {
                string hostedFilesUrl = hostedFilesSource.ToString();

                httpFetcherChain.Add(new CachingResourceFetcher(
                    new RelativeHttpResourceFetcher(new RelativeHttpResourceFetcher.Options()
                    {
                        BasePath = hostedFilesUrl,
                        TelemetryLogger = _options.TelemetryLogger,
                    }),
                    FnGetFullCachePath));
            }

            // Next, try the CloudFront backed location
            if (!string.IsNullOrWhiteSpace(_options.CloudFrontBaseUrl))
            {
                httpFetcherChain.Add(new CachingResourceFetcher(
                    new RelativeHttpResourceFetcher(
                        new RelativeHttpResourceFetcher.Options()
                        {
                            BasePath = _options.CloudFrontBaseUrl,
                            TelemetryLogger = _options.TelemetryLogger,
                        }),
                    FnGetFullCachePath));
            }

            // If we get contents from any of the http based fetchers here,
            // mark that the contents have been cached.
            // See HostedFilesResourceFetcher.Options.DownloadOncePerSession
            return new CallbackResourceFetcher(httpFetcherChain, (path, stream) =>
            {
                if (stream != null)
                {
                    _downloadedResources.Add(path);
                }

                return stream;
            });
        }

        private bool IsHttpBased(Uri uri)
        {
            return uri != null &&
                   uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the contents make an MD5 match against their CloudFront counterpart.
        /// </summary>
        private bool ContentsMatchCloudFront(Stream stream, string relativePath)
        {
            try
            {
                string streamContent = "";
                using (var reader = new StreamReader(stream))
                {
                    streamContent = reader.ReadToEnd();
                }

                var streamMd5 = $"\"{Amazon.S3.Util.AmazonS3Util.GenerateChecksumForContent(streamContent, false)}\"";

                var url = _options.CloudFrontBaseUrl +
                          (_options.CloudFrontBaseUrl.EndsWith("/") ? "" : "/") +
                          relativePath;
                var remoteMd5 = GetCloudFrontETag(url);

                return string.Equals(streamMd5, remoteMd5, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception e)
            {
                Logger.Info($"Error comparing resource against remote source: {relativePath}", e);
                return true; // use the cache version as if it were up to date
            }
        }

        private static string GetCloudFrontETag(string url)
        {
            var httpRequest = WebRequest.Create(url);
            httpRequest.Method = "HEAD";

            using (var response = httpRequest.GetResponse() as HttpWebResponse)
            {
                return response?.Headers["ETag"];
            }
        }
    }
}
