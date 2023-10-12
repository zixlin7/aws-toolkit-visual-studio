﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.ResourceFetchers;

using log4net;

using Version = System.Version;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Manages the lsp version file
    /// </summary>
    public class LspManager : IResourceManager<string>
    {
        public class Options
        {
            /// <summary>
            /// Specifies the compatible version range within which the LSP to be downloaded must be
            /// </summary>
            public VersionRange VersionRange { get; set; }

            /// <summary>
            /// Specifies the name of the file that represents the LSP binary
            /// </summary>
            public string Filename { get; set; }

            /// <summary>
            /// Specifies the name of the Language Server
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// Indicates the parent storage folder where lsp is downloaded 
            /// </summary>
            public string DownloadParentFolder { get; set; }

            public ToolkitContext ToolkitContext { get; set; }
        }

        private static readonly ILog _logger = LogManager.GetLogger(typeof(LspManager));
        private readonly ManifestSchema _manifestSchema;
        private readonly Options _options;

        public LspManager(Options options, ManifestSchema manifestSchema)
        {
            _options = options;
            _manifestSchema = manifestSchema;
        }

        public async Task<string> DownloadAsync(CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                // get latest lsp version from the manifest matching the required toolkit compatible version range
                // and required target content(architecture, platform and files)
                var latestCompatibleLspVersion = GetCompatibleLspVersion();
                var targetContent = GetLspTargetContent(latestCompatibleLspVersion);

                // if the latest version is stored locally already, return that, else fetch the latest version
                var downloadCachePath = GetDownloadPath(latestCompatibleLspVersion.Version, targetContent.Filename);
                if (File.Exists(downloadCachePath))
                {
                    ShowMessage(
                        $"Launching {_options.Name} Language Server v{latestCompatibleLspVersion.Version} from local cache location: {downloadCachePath}");
                    return downloadCachePath;
                }

                // if lsp can be successfully downloaded from remote location, return the download path else attempt fallback location
                var downloadResult =
                    await DownloadFromRemoteAsync(targetContent, latestCompatibleLspVersion.Version, token);
                if (downloadResult)
                {
                    ShowMessage(
                        $"Installing {_options.Name} Language Server v{latestCompatibleLspVersion.Version} to: {downloadCachePath}");
                    return downloadCachePath;
                }

                // if unable to retrieve contents from specified remote location, use the most compatible fallback cached lsp version
                _logger.Info(
                    $"Unable to download {_options.Name} language server version v{latestCompatibleLspVersion.Version}. Attempting to fetch from fallback location");

                var fallbackPath = GetFallbackPath(latestCompatibleLspVersion.Version);
                if (!File.Exists(fallbackPath))
                {
                    throw new ToolkitException(
                        $"Unable to launch language server from fallback location: {fallbackPath}",
                        ToolkitException.CommonErrorCode.UnexpectedError);
                }

                ShowMessage(
                    $"Unable to install {_options.Name} Language Server v{latestCompatibleLspVersion.Version}. Launching a previous version from: {fallbackPath}");
                return fallbackPath;
            }
            catch (Exception e)
            {
                var msg =
                    $"Error installing {_options.Name} language server. The AWS Toolkit may have trouble accessing the {_options.Name} service.";
                _logger.Error(msg, e);
                throw;
            }
        }

        public Task CleanupAsync(CancellationToken token = default)
        {
            _logger.Info($"Cleaning up cached versions of {_options.Name} Language Server");
            if (!_manifestSchema?.Versions.Any() ?? true)
            {
                return Task.CompletedTask;
            }

            // delete de-listed versions in toolkit compatible version range
            DeleteDeListedVersions();

            // delete extra versions in the compatible toolkit version range except highest 2 versions
            DeleteExtraVersions();

            _logger.Info($"Finished cleanup for cached versions of {_options.Name} Language Server");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Displays the message in the toolkit output pane
        /// </summary>
        /// <param name="message"></param>
        private void ShowMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.Info(message);
                _options.ToolkitContext?.ToolkitHost.OutputToHostConsole(message, true);
            }
        }

     

        /// <summary>
        /// Gets latest lsp version matching the toolkit compatible version range, not de-listed and contains required target content(architecture, platform and files)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ToolkitException"></exception>
        private LspVersion GetCompatibleLspVersion()
        {
            if (_manifestSchema == null)
            {
                throw new ToolkitException(
                    "No valid manifest version data was received. An error could have caused this. Please check logs.",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState);
            }

            var latestCompatibleLspVersion = _manifestSchema.Versions
                .Where(ver => IsCompatibleVersion(ver) && HasRequiredTargetContent(ver))
                .OrderByDescending(obj => Version.Parse(obj.Version)).FirstOrDefault();

            if (latestCompatibleLspVersion == null)
            {
                throw new ToolkitException(
                    $"Unable to find a language server that satisfies one or more of these conditions: version in range [{_options.VersionRange.Start} - {_options.VersionRange.End}), matching system's architecture and platform or containing the file: {_options.Filename}  ",
                    ToolkitException.CommonErrorCode.UnsupportedState);
            }

            return latestCompatibleLspVersion;
        }

        /// <summary>
        /// Parses the toolkit  lsp version object retrieved from version manifest to determine lsp contents<see cref="TargetContent"/> eg. filename, url
        /// </summary>
        /// <param name="lspVersion"></param>
        private TargetContent GetLspTargetContent(LspVersion lspVersion)
        {
            // from the language server version object, choose the target matching the user's system architecture and platform
            var lspTarget = GetCompatibleLspTarget(lspVersion);
            if (lspTarget == null)
            {
                throw new ToolkitException(
                    "No language server target found matching the system's architecture and platform",
                    ToolkitException.CommonErrorCode.UnsupportedState);
            }

            // from the matching target, retrieve the contents matching the specified lsp filename required by the toolkit
            var targetContent = GetCompatibleTargetContent(lspTarget);
            if (targetContent == null)
            {
                throw new ToolkitException(
                    $"No matching target content found containing the specified filename: {_options.Filename}",
                    ToolkitException.CommonErrorCode.UnsupportedState);
            }

            return targetContent;
        }

        /// <summary>
        /// Validates the lsp version contains the required toolkit compatible contents: architecture, platform and file
        /// </summary>
        /// <param name="lspVersion"></param>
        private bool HasRequiredTargetContent(LspVersion lspVersion)
        {
            var lspTarget = GetCompatibleLspTarget(lspVersion);
            var targetContent = GetCompatibleTargetContent(lspTarget);

            return targetContent != null;
        }

        /// <summary>
        /// Retrieve the lsp target matching the user's system architecture and platform from language server version object
        /// </summary>
        /// <param name="lspVersion"></param>
        /// <returns></returns>
        private VersionTarget GetCompatibleLspTarget(LspVersion lspVersion)
        {
            var systemArch = _options.ToolkitContext?.ToolkitHost.ProductEnvironment.OperatingSystemArchitecture;
            var lspTarget = lspVersion.Targets?.FirstOrDefault(x =>
                LspInstallUtil.HasMatchingArchitecture(systemArch, x.Arch) &&
                LspInstallUtil.HasMatchingPlatform(x.Platform));
            return lspTarget;
        }

        /// <summary>
        /// Retrieve the target content containing the specified lsp filename required by the toolkit
        /// </summary>
        /// <param name="lspTarget"></param>
        private TargetContent GetCompatibleTargetContent(VersionTarget lspTarget)
        {
            var targetContent = lspTarget?.Contents.FirstOrDefault(x => x.Filename.Equals(_options.Filename));
            return targetContent;
        }

        /// <summary>
        /// Attempts to fetch the specified lsp version from remote server
        /// </summary>
        /// <returns>true if successfully downloaded, else returns false</returns>
        private async Task<bool> DownloadFromRemoteAsync(TargetContent targetContent,
            string lspVersion, CancellationToken token)
        {
            // verify the hash of the file matches the expected hash for the chosen lsp version
            Task<bool> Validate(Stream stream)
            {
                var hash = LspInstallUtil.GetHash(stream);
                var expectedHash = GetExpectedHash(targetContent);

                return Task.FromResult(string.Equals(hash, expectedHash));
            }

            var fetcher = CreateLspFetcher(lspVersion, targetContent.Filename, Validate);

            // if lsp can be successfully downloaded, return true else false
            using (var stream = await fetcher.GetAsync(targetContent.Url, token))
            {
                // Note: caching occurs as part of the fetcher
                return stream != null;
            }
        }
         
        /// <summary>
        /// Create LSP fetcher using the manifest specified location
        /// </summary>
        private IResourceFetcher CreateLspFetcher(string version, string filename, Func<Stream, Task<bool>> validate)
        {
            var options = new LspFetcher.Options()
            {
                Filename = filename,
                ResourceValidator = validate,
                DownloadedCachePath = GetDownloadPath(version, filename),
                Version = version,
                TelemetryLogger = _options.ToolkitContext.TelemetryLogger
            };

            return new LspFetcher(options);
        }

        private string GetDownloadPath(string version, string filename)
        {
            return Path.Combine(_options.DownloadParentFolder, version, filename);
        }

        private string GetExpectedHash(TargetContent targetContent)
        {
            var expectedHashKeyPair = targetContent.Hashes.FirstOrDefault(x => x.Contains("sha384"));
            // extract the hash value from string in format - sha384:abcbchdhdbchd
            if (expectedHashKeyPair == null || expectedHashKeyPair.IndexOf(':') == -1)
            {
                _logger.Error("Error parsing hash key pair, expected hash key pair in format: sha384:1234");
                return null;
            }

            var expectedHash = expectedHashKeyPair.Substring(expectedHashKeyPair.IndexOf(':') + 1);
            return expectedHash;
        }

        /// <summary>
        /// Determine if given lsp version is toolkit compatible i.e in version range and not de-listed
        /// </summary>
        /// <param name="lspVersion"></param>
        private bool IsCompatibleVersion(LspVersion lspVersion)
        {
            Version.TryParse(lspVersion.Version, out var version);
            return _options.VersionRange.ContainsVersion(version) && !lspVersion.IsDelisted;
        }


        /// <summary>
        /// Determines list of toolkit compatible versions from the manifest i.e in version range and not de-listed
        /// </summary>
        private IList<Version> GetCompatibleVersions()
        {
            return _manifestSchema.Versions.Where(IsCompatibleVersion).Select(x => Version.Parse(x.Version)).ToList();
        }

        /// <summary>
        /// Get fallback location representing the most compatible cached lsp version
        /// </summary>
        /// <param name="lspVersion">the lsp version with which the fallback version must be the most compatible to</param>
        private string GetFallbackPath(string lspVersion)
        {
            var compatibleVersions = GetCompatibleVersions();
            var fallbackFolder = LspInstallUtil.GetFallbackVersionFolder(_options.DownloadParentFolder,
                lspVersion, compatibleVersions);

            if (fallbackFolder == null)
            {
                ShowMessage($"AWS Toolkit was unable to find a compatible version of {_options.Name} Language Server.");
                throw new ToolkitException(
                    $"AWS Toolkit was unable to find a compatible version of {_options.Name} Language Server.",
                    ToolkitException.CommonErrorCode.UnexpectedError);
            }

            return Path.Combine(fallbackFolder, _options.Filename);
        }

        private void DeleteDeListedVersions()
        {
            var compatibleVersions = GetCompatibleVersions();
            var cachedVersions = LspInstallUtil.GetAllCachedVersions(_options.DownloadParentFolder);

            // delete de-listed versions in the toolkit compatible version range
            var deListedVersions = cachedVersions
                .Where(x => !compatibleVersions.Contains(x) && _options.VersionRange.ContainsVersion(x)).ToList();

            _logger.Info($"Cleaning up {deListedVersions.Count} cached de-listed versions for {_options.Name} Language Server");
            deListedVersions.ForEach(DeleteCachedVersion);
        }

        private void DeleteExtraVersions()
        {
            var cachedVersions = LspInstallUtil.GetAllCachedVersions(_options.DownloadParentFolder);

            // delete extra versions in the compatible toolkit version range except highest 2 versions
            var extraVersions = cachedVersions.Where(x => _options.VersionRange.ContainsVersion(x))
                .OrderByDescending(x => x).Skip(2).ToList();

            _logger.Info($"Cleaning up {extraVersions.Count} cached versions for {_options.Name} Language Server");
            extraVersions.ForEach(DeleteCachedVersion);
        }

        private void DeleteCachedVersion(Version version)
        {
            var path = Path.Combine(_options.DownloadParentFolder, version.ToString(),
                _options.Filename);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error cleaning up cached version: {path}", e);
            }
        }
    }
}
