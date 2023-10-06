using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                $"lsp/manifest/{_options.CompatibleMajorVersion}");
        }

        private async Task<ChainedResourceFetcher> CreateResourceFetcherChainAsync(string relativePath)
        {
            var lspSettings = await _settingsRepository.GetLspSettingsAsync();
            var lspManifestSource = GetManifestLocationAsUri(lspSettings);

            var downloadCacheFetcher = new RelativeFileResourceFetcher(DownloadedCacheFolder);

            var fetcherChain = new ChainedResourceFetcher();
          
            // If toolkit is configured with a local version manifest location, use that first
            if (lspManifestSource != null && lspManifestSource.IsFile)
            {
                var localLocationFetcher = new RelativeFileResourceFetcher(lspManifestSource.LocalPath);
                // Validate the contents of the file fetched from the local path before returning it
                var validatedLocalLocationFetcher = new ConditionalResourceFetcher(localLocationFetcher,
                        _options.ResourceValidator ?? (stream => Task.FromResult(true)));
                    fetcherChain.Add(validatedLocalLocationFetcher);
            }

            // Next use the download cache, if it is valid and contains the latest version w.r.t the online counterpart
            var validatedDownloadCacheFetcher = new ConditionalResourceFetcher(downloadCacheFetcher, async (stream) =>
            {
                // TODO:  Check if there is a different version online by comparing ETags, and opt to use that instead of the download cache version.

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


            // TODO: Fetch manifest from various online sources: CloudFront, S3. If the resource is obtained this way, it is also cached.

            fetcherChain.Add(validatedDownloadCacheFetcher);
            return fetcherChain;
        }

        public async Task<Stream> GetAsync(string relativePath, CancellationToken token = default)
        {
            var fetcherChain = await CreateResourceFetcherChainAsync(relativePath);

            return await fetcherChain.GetAsync(relativePath, token);
        }

        /// <summary>
        /// Converts the user-configured version manifest path value to a Uri, which
        /// could be null (use system defaults), or a Uri that points to
        /// either a location on disk or online.
        /// </summary>
        private Uri GetManifestLocationAsUri(ILspSettings lspSettings)
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
