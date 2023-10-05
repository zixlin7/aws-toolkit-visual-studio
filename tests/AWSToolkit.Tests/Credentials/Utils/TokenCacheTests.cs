using System;
using System.IO;

using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class TokenCacheTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();

        private static readonly string _sampleSsoSessionName = new SonoCredentialIdentifier
            (SonoCredentialProviderFactory.CodeCatalystProfileName).ToDefaultSessionName();

        private static readonly string _sampleSsoStartUrl = SonoProperties.StartUrl;

        private static readonly string _expectedFilename = "5e3b6d0e0a5a1917f5c7deba8b9a7745877ed003.json";

        [Fact]
        public void GetCacheFileName()
        {
            Assert.Equal(_expectedFilename, TokenCache.GetCacheFilename(_sampleSsoStartUrl, _sampleSsoSessionName));
        }

        [Fact]
        public void RemoveCacheFile()
        {
            var cachePath = GetFullCachePath();
            File.WriteAllText(cachePath, "a sample file that will be deleted");

            TokenCache.RemoveCacheFile(_sampleSsoStartUrl, _sampleSsoSessionName, _testLocation.TestFolder);

            Assert.False(File.Exists(cachePath));
        }

        [Fact]
        public void RemoveCacheFile_FileNotExist()
        {
            TokenCache.RemoveCacheFile(_sampleSsoStartUrl, _sampleSsoSessionName, _testLocation.TestFolder);

            Assert.False(File.Exists(GetFullCachePath()));
        }

        public void Dispose()
        {
            _testLocation?.Dispose();
        }

        private string GetFullCachePath()
        {
            return Path.Combine(_testLocation.TestFolder, _expectedFilename);
        }
    }
}
