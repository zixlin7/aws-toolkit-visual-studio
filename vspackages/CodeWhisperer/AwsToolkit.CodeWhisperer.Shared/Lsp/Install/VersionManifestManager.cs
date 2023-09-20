using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.ResourceFetchers;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Manages the lsp version manifest file 
    /// </summary>
    public class VersionManifestManager: IResourceManager<ManifestSchema>
    {
        // Each toolkit release is expected to be compatible per major version (eg: 0.x, 1.x, ...) of the version manifest schema
        public const int CompatibleMajorVersion = 0;
        public const string ManifestFile = "lspManifest.json";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(VersionManifestManager));
        private readonly IResourceFetcher _versionManifestFetcher;

        public static VersionManifestManager Create(ICodeWhispererSettingsRepository settingsRepository)
        {
            var fetcher = CreateVersionManifestFetcher(settingsRepository);
            return new VersionManifestManager(fetcher);
        }

        internal VersionManifestManager(IResourceFetcher versionManifestFetcher)
        {
            _versionManifestFetcher = versionManifestFetcher;
        }

        /// <summary>
        /// Creates a resource fetcher that gets the version manifest
        /// </summary>
        private static IResourceFetcher CreateVersionManifestFetcher(ICodeWhispererSettingsRepository settingsRepository)
        {
            var options = new VersionManifestFetcher.Options()
            {
                ResourceValidator = IsValidAsync,
                CompatibleMajorVersion = CompatibleMajorVersion
            };

            return new VersionManifestFetcher(options, settingsRepository);
        }

        public async Task<ManifestSchema> DownloadAsync(CancellationToken token = default)
        {
            try
            {
                using (var stream = await _versionManifestFetcher.GetAsync("lspManifest.json", token))
                using (var streamCopy = new MemoryStream())
                {
                    if (stream == null)
                    {
                        throw new ToolkitException("No manifest data was received. An error could have caused this. Please check logs.", ToolkitException.CommonErrorCode.UnsupportedState);
                    }
                    await stream.CopyToAsync(streamCopy);
                    streamCopy.Position = 0;

                    ManifestSchema schema = null;
                    // ManifestSchemaUtil.Load destroys the stream, give it a copy
                    using (var endpointsStream = new MemoryStream(streamCopy.GetBuffer()))
                    {
                        schema = await ManifestSchemaUtil.LoadAsync(endpointsStream);
                        if (schema == null)
                        {
                            throw new ToolkitException("Error retrieving version manifest data.", ToolkitException.CommonErrorCode.UnsupportedState);
                        }
                    }

                    return schema;
                }
            }
            catch (Exception e)
            {
                _logger.Error(
                    "Error downloading version manifest data. The Toolkit may have trouble accessing the CodeWhisperer service.",
                    e);
                throw;
            }
        }

        /// <summary>
        /// Indicates whether or not the stream contains a valid lsp version manifest file
        /// </summary>
        private static async Task<bool> IsValidAsync(Stream stream)
        {
            try
            {
                var manifestSchema = await ManifestSchemaUtil.LoadAsync(stream);
                return manifestSchema != null && new Version(manifestSchema.SchemaVersion).Major == CompatibleMajorVersion;
            }
            catch
            {
                return false;
            }
        }
    }
}
