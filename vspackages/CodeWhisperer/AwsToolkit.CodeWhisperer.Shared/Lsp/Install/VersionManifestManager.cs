using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.ResourceFetchers;

using AwsToolkit.VsSdk.Common.Settings;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Manages the lsp version manifest file 
    /// </summary>
    public class VersionManifestManager : IResourceManager<ManifestSchema>
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(VersionManifestManager));
        private readonly IResourceFetcher _versionManifestFetcher;
        private readonly VersionManifestOptions _options;

        public static VersionManifestManager Create(VersionManifestOptions options, ILspSettingsRepository settingsRepository)
        {
            var fetcher = CreateVersionManifestFetcher(options, settingsRepository);
            return new VersionManifestManager(options, fetcher);
        }

        internal VersionManifestManager(VersionManifestOptions options, IResourceFetcher versionManifestFetcher)
        {
            _options = options;
            _versionManifestFetcher = versionManifestFetcher;
        }

        /// <summary>
        /// Creates a resource fetcher that gets the version manifest
        /// </summary>
        private static IResourceFetcher CreateVersionManifestFetcher(VersionManifestOptions managerOptions,
            ILspSettingsRepository settingsRepository)
        {
            // Indicates whether or not the stream contains a valid lsp version manifest file
            async Task<bool> IsValidAsync(Stream stream)
            {
                try
                {
                    var manifestSchema = await ManifestSchemaUtil.LoadAsync(stream);
                    return manifestSchema != null &&
                           new Version(manifestSchema.SchemaVersion).Major == managerOptions.MajorVersion;
                }
                catch
                {
                    return false;
                }
            }

            var options = new VersionManifestFetcher.Options()
            {
                ResourceValidator = IsValidAsync, CompatibleMajorVersion = managerOptions.MajorVersion
            };

            return new VersionManifestFetcher(options, settingsRepository);
        }

        public async Task<ManifestSchema> DownloadAsync(CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                using (var stream = await _versionManifestFetcher.GetAsync(_options.FileName, token))
                using (var streamCopy = new MemoryStream())
                {
                    if (stream == null)
                    {
                        throw new ToolkitException(
                            $"No {_options.Name} Language Server version manifest data was received. An error could have caused this. Please check AWS Toolkit logs.",
                            ToolkitException.CommonErrorCode.UnsupportedState);
                    }

                    await stream.CopyToAsync(streamCopy);
                    streamCopy.Position = 0;

                    ManifestSchema schema = null;
                    // ManifestSchemaUtil.Load destroys the stream, give it a copy
                    using (var manifestStream = new MemoryStream(streamCopy.GetBuffer()))
                    {
                        schema = await ManifestSchemaUtil.LoadAsync(manifestStream);
                        if (schema == null)
                        {
                            throw new ToolkitException($"Error parsing {_options.Name} Language Server version manifest data.",
                                ToolkitException.CommonErrorCode.UnsupportedState);
                        }
                    }

                    return schema;
                }
            }
            catch (Exception e)
            {
                _logger.Error(
                    $"Error downloading {_options.Name} Language Server version manifest data. The AWS Toolkit may have trouble accessing the {_options.Name} service.",
                    e);
                throw;
            }
        }

        public Task CleanupAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
