using System.IO;
using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;

using Xunit;

using Amazon.AWSToolkit.Util.Tests.ResourceFetchers;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Manifest
{
    public class VersionManifestFetcherTests : IDisposable
    {
        private const string _versionManifestSampleData = "{\r\n    \"schemaVersion\": \"0.1\",\r\n}";
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly VersionManifestFetcher.Options _options;
        private readonly VersionManifestFetcher _sut;
        private readonly string _versionManifestFileName = "lspManifest.json";

        public VersionManifestFetcherTests()
        {
            _options = new VersionManifestFetcher.Options()
            {
                DownloadedCacheParentFolder = _fixture.TestLocation.OutputFolder
            };
            _sut = new VersionManifestFetcher(_options, _settingsRepository);
            Directory.CreateDirectory(_sut.DownloadedCacheFolder);
        }

        [Fact]
        public async Task Get_WhenDownloadCacheEmptyAsync()
        {
            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.Null(stream);

            Assert.False(CacheExists());
        }

        [Fact]
        public async Task Get_WhenDownloadCacheNotEmptyAsync()
        {
            File.WriteAllText(Path.Combine(_sut.DownloadedCacheFolder, _versionManifestFileName),
                _versionManifestSampleData);

            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_versionManifestSampleData, text);
        }

        [Fact]
        public async Task Get_WhenSettingsOverridenToLocalFolderAsync()
        {
            Assert.False(CacheExists());

            var localManifestPath = $"input/{_versionManifestFileName}";

            _fixture.WriteToFile(_versionManifestSampleData, localManifestPath);
            _settingsRepository.Settings.VersionManifestFolder = _fixture.TestLocation.InputFolder;

            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_versionManifestSampleData, text);

            // verify that overridden local copy does not get cached
            Assert.False(CacheExists());
        }


        [Fact]
        public async Task Get_WhenValidationFailsAsync()
        {
            _options.ResourceValidator = s => Task.FromResult(false);

            File.WriteAllText(Path.Combine(_sut.DownloadedCacheFolder, _versionManifestFileName),
                _versionManifestSampleData);

            Assert.True(CacheExists());

            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.Null(stream);

            //verify cache is deleted if validation of cached copy fails
            Assert.False(CacheExists());
        }

        public void Dispose()
        {
            _fixture?.Dispose();
        }

        private bool CacheExists()
        {
            return File.Exists(Path.Combine(_sut.DownloadedCacheFolder, _versionManifestFileName));
        }
    }
}
