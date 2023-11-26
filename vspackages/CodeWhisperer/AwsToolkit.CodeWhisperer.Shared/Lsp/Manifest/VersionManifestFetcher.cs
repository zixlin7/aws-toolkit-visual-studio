using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.Runtime.Internal.Settings;

using AwsToolkit.VsSdk.Common.Settings;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest
{
    /// <summary>
    /// Retrieves version manifest file based on an order of precedence
    /// that attempts to keep the Toolkit/IDE stable if contents are
    /// unavailable or corrupt.
    /// </summary>
    public class VersionManifestFetcher : IResourceFetcher
    {
        public class Options
        {

            /// <summary>
            /// Specifies the name of the Language Server associated with the version manifest
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// Specifies the major version of the manifest schema with which a given toolkit release is compatible with eg. 0.x, 1.x
            /// </summary>
            public int CompatibleMajorVersion { get; set; } = 0;

            /// <summary>
            /// Parent folder where lsp version manifest is stored after download
            /// </summary>
            public string DownloadedCacheParentFolder { get; set; } = PersistenceManager.GetSettingsStoreFolder();

            /// <summary>
            /// (Optional) Callback that verifies if the contents can be considered valid,
            /// or if contents should be retrieved from a fallback location.
            /// </summary>
            public Func<Stream, Task<bool>> ResourceValidator { get; set; } = null;

            /// <summary>
            /// CloudFront-backed base location to fetch manifest from
            /// </summary>
            public string CloudFrontBaseUrl { get; set; }

            public ToolkitContext ToolkitContext { get; set; }
        }

        private static readonly ILog _logger = LogManager.GetLogger(typeof(VersionManifestFetcher));
        private readonly ILspSettingsRepository _settingsRepository;
        private readonly Options _options;

        /// <summary>
        /// Location where lsp version manifest is stored to when it is downloaded
        /// </summary>
        public string DownloadedCacheFolder { get; }

        public VersionManifestFetcher(Options options, ILspSettingsRepository settingsRepository)
        {
            _options = options;
            _settingsRepository = settingsRepository;
            DownloadedCacheFolder = Path.Combine(_options.DownloadedCacheParentFolder,
                "lsp", "manifest", _options.Name, _options.CompatibleMajorVersion.ToString());
        }

        public async Task<Stream> GetAsync(string relativePath, CancellationToken token = default)
        {
            var lspSettings = await _settingsRepository.GetLspSettingsAsync();
            var lspManifestSource = GetManifestLocationAsUri(lspSettings);


            // If toolkit is configured with a local version manifest location, use that first
            if (lspManifestSource != null && lspManifestSource.IsFile)
            {
                var localLocationFetcher = new RelativeFileResourceFetcher(lspManifestSource.LocalPath);
                // Validate the contents of the file fetched from the local path before returning it
                var validatedLocalLocationFetcher = CreateConditionalResourceFetcher(localLocationFetcher);

                return await validatedLocalLocationFetcher.GetAsync(relativePath, token);
            }

            // if cache has the latest contents(by comparing e-tags with online version) and is valid use that, else fetch from remote
            try
            {
                var cacheStream = await GetResourceFromCacheAsync(relativePath, token);

                // if a remote location does not exist, default to content in the download cache
                if (string.IsNullOrWhiteSpace(_options.CloudFrontBaseUrl))
                {
                    return cacheStream;
                }

                var cloudFrontFullUrl = GetCloudFrontFullUrl(relativePath);
                var cachedEtag = GetEtagForManifest(lspSettings, cloudFrontFullUrl)?.Etag;

                // only use cached content if it is valid and a cached etag exists
                var etagToRequest = cacheStream != null && !string.IsNullOrWhiteSpace(cachedEtag) ? cachedEtag : null;

                // fetch contents from remote if new version exists
                var response = await GetLatestFromRemoteAsync(cloudFrontFullUrl, etagToRequest);
                if (response == null)
                {
                    _logger.Info("Version manifest cache has the latest contents");
                    // cache content is latest, return that
                    return cacheStream;
                }

                // fetch latest contents from remote location and cache it
                return await ValidateRemoteResourceAsync(relativePath, response);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to fetch version manifest from remote location", ex);
                return null;
            }
        }

        /// <summary>
        /// Fetch contents from local cache
        /// </summary>
        private async Task<Stream> GetResourceFromCacheAsync(string relativePath, CancellationToken token = default)
        {
            var downloadCacheFetcher = new RelativeFileResourceFetcher(DownloadedCacheFolder);
            var validatedDownloadCacheFetcher = new ConditionalResourceFetcher(downloadCacheFetcher, async (stream) =>
            {
                // verify if cached version is parseable and valid, if it corrupted delete the cached fallback version
                var result = _options.ResourceValidator == null || await _options.ResourceValidator.Invoke(stream);
                if (!result)
                {
                    var path = Path.Combine(DownloadedCacheFolder, relativePath);
                    DeleteFile(path);
                    return false;
                }

                return true;
            });
            try
            {
                return await validatedDownloadCacheFetcher.GetAsync(relativePath, token);
            }
            catch (Exception e)
            {
                _logger.Error("Error fetching resource from local cache", e);
                return null;
            }
        }

        private string GetCloudFrontFullUrl(string relativePath)
        {
            return $"{_options.CloudFrontBaseUrl}/{_options.CompatibleMajorVersion}/{relativePath}";
        }

        private ConditionalResourceFetcher CreateConditionalResourceFetcher(IResourceFetcher resourceFetcher)
        {
            return new ConditionalResourceFetcher(resourceFetcher,
                _options.ResourceValidator ?? (stream => Task.FromResult(true)));
        }

        /// <summary>
        /// Requests for new content but additionally uses the given E-Tag.
        /// If no E-Tag is given, it behaves as a normal request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="etag"></param>
        /// <returns> response of the request. If response is null, it implies the provided E-Tag matched the server's, so no new content was in response.</returns>
        private async Task<HttpWebResponse> GetLatestFromRemoteAsync(string url, string etag)
        {
           
            var response = await GetResponseFromRemoteAsync(url, etag);
            return response.StatusCode == HttpStatusCode.NotModified ? null : response;
        }

        private static async Task<HttpWebResponse> GetResponseFromRemoteAsync(string url, string etag)
        {
            url = url.Replace(@"\", "/");

            var uri = new Uri(url);
            var webRequest = WebRequest.Create(uri);
            // add etag header to verify if contents match local cache
            if (!string.IsNullOrWhiteSpace(etag))
            {
                webRequest.Headers["If-None-Match"] = etag;
            }

            try
            {
                return (HttpWebResponse) await webRequest.GetResponseAsync();
            }
            catch (WebException ex)
            when(ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
            {
                return (HttpWebResponse) ex.Response;
            }
        }

        /// <summary>
        /// Validates the latest contents retrieved from remote location and if valid, updates cache(content + etag)
        /// </summary>
        private async Task<Stream> ValidateRemoteResourceAsync(string relativePath, HttpWebResponse response)
        {
            var cloudFrontFullUrl = GetCloudFrontFullUrl(relativePath);
            var latestStream = response.GetResponseStream();
            var etag = response.Headers["Etag"];

            var validatedStream = await GetValidatedRemoteStreamAsync(relativePath, latestStream);

            if (validatedStream != null)
            {
                // cache the etag if contents  are valid
                await CacheEtagAsync(etag, cloudFrontFullUrl);
            }

            return validatedStream;
        }

        /// <summary>
        /// Validates and caches the remote contents retrieved
        /// </summary>
        private async Task<Stream> GetValidatedRemoteStreamAsync(string relativePath, Stream stream)
        {
            string FnGetFullCachePath(string path) => Path.Combine(DownloadedCacheFolder, path);

            // validate the stream and then cache it
            var passThroughRemoteFetcher = new PassThroughResourceFetcher(stream);
            var validatedRemoteFetcher =
                new CachingResourceFetcher(CreateConditionalResourceFetcher(passThroughRemoteFetcher),
                    FnGetFullCachePath);

            return await validatedRemoteFetcher.GetAsync(relativePath);
        }

        /// <summary>
        /// Updates settings to cache/store the etag for specified manifest
        /// </summary>
        private async Task CacheEtagAsync(string etag, string cloudFrontUrl)
        {
            try
            {
                var settings = await _settingsRepository.GetLspSettingsAsync();
                var cachedEntry = GetEtagForManifest(settings, cloudFrontUrl);

                if (cachedEntry == null)
                {
                    cachedEntry = new ManifestCachedEtag()
                    {
                        Etag = etag,
                        ManifestUrl = cloudFrontUrl
                    };
                    settings.ManifestCachedEtags.Add(cachedEntry);

                }
                else
                {
                    cachedEntry.Etag = etag;
                    cachedEntry.ManifestUrl = cloudFrontUrl;
                }

                await _settingsRepository.SaveLspSettingsAsync(settings);

            }
            catch (Exception e)
            {
                _logger.Error($"Error caching etag for version manifest: {cloudFrontUrl}", e);
            }
        }

        /// <summary>
        /// Gets etag associated with specified manifest url from cached e-tags list
        /// </summary>
        private ManifestCachedEtag GetEtagForManifest(LspSettings settings, string manifestUrl)
        {
            return settings?.ManifestCachedEtags?
                .FirstOrDefault(x =>
                    x != null && string.Equals(x.ManifestUrl, manifestUrl));
        }


        /// <summary>
        /// Converts the user-configured version manifest path value to a Uri, which
        /// could be null (use system defaults), or a Uri that points to
        /// either a location on disk or online.
        /// </summary>
        private Uri GetManifestLocationAsUri(LspSettings lspSettings)
        {
            var location = lspSettings?.VersionManifestFolder;

            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            // eg: "file://<some-file>" , "http://<some-url>"
            return new Uri(location);
        }

        private void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error deleting file: {path}", e);
            }
        }
    }
}
