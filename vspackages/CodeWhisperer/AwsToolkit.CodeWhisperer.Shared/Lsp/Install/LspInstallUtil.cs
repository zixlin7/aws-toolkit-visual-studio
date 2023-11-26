using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public class LspInstallUtil
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LspInstallUtil));

        /// <summary>
        /// Get all cached versions under the download directory
        /// </summary>
        /// <param name="downloadParentFolder"></param>
        public static IEnumerable<Version> GetAllCachedVersions(string downloadParentFolder)
        {
            var searchPattern = "*.*.*";
            var cachedVersions = Directory.GetDirectories(downloadParentFolder,
                    searchPattern, SearchOption.TopDirectoryOnly)
                .Select(GetVersionedName).Where(x => x != null);
            return cachedVersions;
        }


        /// <summary>
        /// Get version represented by the folder contained in this path
        /// </summary>
        private static Version GetVersionedName(string path)
        {
            var dir = new DirectoryInfo(path);
            Version.TryParse(dir.Name, out var result);
            return result;
        }

        public static bool HasMatchingArchitecture(string systemArchitecture, string lspArchitecture)
        {
            return string.Equals(systemArchitecture, lspArchitecture, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetHash(Stream stream)
        {
            // as per spec: use sha384 hash to verify validity of LSP binary
            using (var sha384 = SHA384.Create())
            {
                var val = sha384.ComputeHash(stream);
                return BitConverter.ToString(val).Replace("-", String.Empty);
            }
        }

        public static bool HasMatchingPlatform(string platform)
        {
            // assumption: the underlying OS for users running the toolkit is expected to be Windows
            var systemPlatform = OSPlatform.Windows.ToString();
            var lspPlatform = GetLspPlatform(platform);
            return string.Equals(systemPlatform, lspPlatform, StringComparison.OrdinalIgnoreCase);
        }

        public static void DeleteFile(string path)
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

        /// <summary>
        /// Creates <see cref="RecordLspInstallerArgs"/> using provided params
        /// </summary>
        public static RecordLspInstallerArgs CreateRecordLspInstallerArgs(LspInstallResult result, long milliseconds)
        {
            var args = new RecordLspInstallerArgs()
            {
                Duration = milliseconds,
                LanguageServerVersion = result?.Version
            };
            if (result != null)
            {
                args.Location = result.Location;
            }
            return args;
        }

        private static string GetLspPlatform(string platform)
        {
            switch (platform)
            {
                case "mac":
                    return OSPlatform.OSX.ToString().ToLower();
                default:
                    return platform;
            }
        }
    }
}
