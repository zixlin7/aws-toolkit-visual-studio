using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AwsToolkit.Telemetry.Events.Core;

using log4net;

using Path = System.IO.Path;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Retrieves lsp version file based on an order of precedence
    /// that attempts to keep the Toolkit/IDE stable if contents are
    /// unavailable or corrupt.
    /// </summary>
    public class LspFetcher : IResourceFetcher
    {
        public class Options
        {
            /// <summary>
            /// Specifies the version of the LSP to be downloaded
            /// </summary>
            public string Version { get; set; } = "0.0.0";

            /// <summary>
            /// Specifies the name of the file that represents the LSP binary
            /// </summary>
            public string Filename { get; set; }

            /// <summary>
            /// Path where LSP is stored after download
            /// </summary>
            public string DownloadedCachePath { get; set; }

            /// <summary>
            /// Temp parent folder where LSP is stored before validation is performed
            /// </summary>
            public string TempCacheFolderPath { get; set; } =
                Path.Combine(Path.GetTempPath(), "AwsToolkit", "lsp", "downloads", Path.GetRandomFileName());

            /// <summary>
            /// (Optional) Callback that verifies if the contents can be considered valid,
            /// or if contents should be retrieved from a fallback location.
            /// </summary>
            public Func<Stream, Task<bool>> ResourceValidator { get; set; } = null;

            public ITelemetryLogger TelemetryLogger { get; set; }
        }

        private static readonly ILog _logger = LogManager.GetLogger(typeof(LspFetcher));
        private readonly Options _options;

        public string TempCachePath { get; set; }

        public LspFetcher(Options options)
        {
            _options = options;
            TempCachePath = Path.Combine(_options.TempCacheFolderPath, _options.Version, options.Filename);
        }

        public async Task<Stream> GetAsync(string path, CancellationToken token = default)
        {
            var fetcherChain = CreateResourceFetcherChain(new Uri(path));
            return await fetcherChain.GetAsync(path, token);
        }

        private IResourceFetcher CreateResourceFetcherChain(Uri source)
        {
            var fetcherChain = new ChainedResourceFetcher();

            // If the lsp location is a file, use the file fetcher
            if (source.IsFile)
            {
                var fileFetcher = new FileResourceFetcher();
                fetcherChain.Add(fileFetcher);
            }
            // else if the lsp location is a remote location, use the http fetcher
            else if (source.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var httpFetcher =
                    new HttpResourceFetcher(
                        new HttpResourceFetcherOptions() { TelemetryLogger = _options.TelemetryLogger });
                fetcherChain.Add(httpFetcher);
            }

            string FnGetTempCachePath(string path) => TempCachePath;
            var tempDownloadFetcher = new CachingResourceFetcher(fetcherChain,
                FnGetTempCachePath);

            // Fetch and validate the contents by caching it to a temp location first
            var validatedFetcher = new ConditionalResourceFetcher(tempDownloadFetcher, async (stream) =>
            {
                // verify if contents fetched are valid and delete the temp copy
                var result = _options.ResourceValidator == null || await _options.ResourceValidator.Invoke(stream);
                DeleteFolder(_options.TempCacheFolderPath);
                return result;
            });

            string FnGetFullCachePath(string path) => _options.DownloadedCachePath;

            // if contents are successfully validated, cache them to the specified download location
            var downloadCacheFetcher = new CachingResourceFetcher(validatedFetcher,
                FnGetFullCachePath);
            return downloadCacheFetcher;
        }

        private void DeleteFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error deleting file: {path}", e);
            }
        }
    }
}
