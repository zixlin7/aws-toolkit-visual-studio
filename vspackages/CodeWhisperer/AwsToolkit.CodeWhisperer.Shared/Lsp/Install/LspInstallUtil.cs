using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public class LspInstallUtil
    {
        /// <summary>
        /// Determine most compatible fallback lsp version folder 
        /// </summary>
        public static string GetFallbackVersionFolder(string downloadParentFolder, string expectedVersionString)
        {
            // determine all folders containing lsp versions in the fallback parent folder
            var searchPattern = "*.*.*";
            var cachedVersions = Directory.GetDirectories(downloadParentFolder,
                    searchPattern, SearchOption.AllDirectories)
                .Select(GetVersionedName);

            // filter folders containing toolkit compatible lsp versions and sort them to determine the most compatible lsp version
            var expectedVersion = Version.Parse(expectedVersionString);
            var sortedVersions = cachedVersions.Where(x => LspConstants.LspCompatibleVersionRange.ContainsVersion(x) && x <= expectedVersion).OrderByDescending(x => x);
            var fallbackVersionFolder = sortedVersions.FirstOrDefault()?.ToString();
            if (string.IsNullOrWhiteSpace(fallbackVersionFolder))
            {
                return null;
            }

            return Path.Combine(downloadParentFolder, fallbackVersionFolder);
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
            using (var sha384 = SHA384.Create())
            {
                var hash = Convert.ToBase64String(sha384.ComputeHash(stream));
                return hash;
            }
        }

        public static bool HasMatchingPlatform(string platform)
        {
            // assumption: the underlying OS for users running the toolkit is expected to be Windows
            var systemPlatform = OSPlatform.Windows.ToString();
            var lspPlatform = GetLspPlatform(platform);
            return string.Equals(systemPlatform, lspPlatform, StringComparison.OrdinalIgnoreCase);
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
