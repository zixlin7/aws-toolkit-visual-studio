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

        private static readonly string _sampleSsoSessionName = "session-name";

        private static readonly string _sampleSsoStartUrl = SonoProperties.StartUrl;

        private static readonly string _expectedFilename = "1b3b78a276e7c20461d218e42fd5f24d7e671635.json";

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
