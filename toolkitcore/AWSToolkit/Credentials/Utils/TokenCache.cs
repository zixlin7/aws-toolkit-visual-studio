using System;
using System.IO;
using System.Text;

using Amazon.Util;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class TokenCache
    {
        /// <summary>
        /// Remove an SSO Cache file
        ///
        /// TODO : Revisit whether or not this can reside in the AWS SDK, so that we do not have to duplicate the
        /// filename generation logic.
        /// </summary>
        public static void RemoveCacheFile(
            string startUrl,
            string session,
            string baseCacheDirectory)
        {
            var cachePath = BuildCacheFileFullPath(startUrl, session, baseCacheDirectory);

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }

        // --------- Code below this point is copied from the AWS SDK with little modification ---------
        // SDK Class: Amazon.Runtime.Credentials.Internal.SSOTokenFileCache

        private static string BuildCacheFileFullPath(
          string startUrl,
          string session,
          string ssoCacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(ssoCacheDirectory))
                ssoCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "sso", "cache");
            string cacheFilename = GetCacheFilename(startUrl, session);
            return Path.Combine(ssoCacheDirectory, cacheFilename);
        }

        /// <summary>
        /// Determines the file name of the Token Cache, regardless of folder.
        /// If <see cref="P:Amazon.Runtime.Credentials.Internal.SsoToken.Session" /> is set, than that will be used to calculate the filename,
        /// otherwise <see cref="P:Amazon.Runtime.Credentials.Internal.SsoToken.StartUrl" /> is used.
        /// </summary>
        /// <returns>
        /// The filename to be used for a <see cref="T:Amazon.Runtime.Credentials.Internal.SsoToken" />
        /// </returns>
        public static string GetCacheFilename(string startUrl, string session) => GetCacheFilename(startUrl, session, CryptoUtilFactory.CryptoInstance);

        private static string GetCacheFilename(string startUrl, string session, ICryptoUtil cryptoUtil) => (!string.IsNullOrEmpty(session) ? GenerateSha1Hash(session, cryptoUtil) : GenerateSha1Hash(startUrl, cryptoUtil)) + ".json";

        /// <summary>Generate a SHA1 hash for the given text</summary>
        /// <param name="text">Text to generate a hash for</param>
        /// <returns>
        /// SHA1 hash for <paramref name="text" />
        /// </returns>
        private static string GenerateSha1Hash(string text, ICryptoUtil cryptoUtil) => AWSSDKUtils.ToHex(cryptoUtil.ComputeSHA1Hash(Encoding.UTF8.GetBytes(text ?? "")), true);
    }
}
