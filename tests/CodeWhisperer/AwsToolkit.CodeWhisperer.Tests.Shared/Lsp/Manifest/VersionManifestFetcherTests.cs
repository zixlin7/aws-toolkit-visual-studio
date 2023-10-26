using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;

using Xunit;

using Amazon.AWSToolkit.Util.Tests.ResourceFetchers;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AWSToolkit.Tests.Common.Context;

using AwsToolkit.VsSdk.Common.Settings;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Manifest
{
    public class VersionManifestFetcherTests : IDisposable
    {
        private const string _versionManifestSampleData = "{\r\n    \"schemaVersion\": \"0.1\",\r\n}";
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly VersionManifestFetcher.Options _options;
        private readonly VersionManifestFetcher _sut;
        private readonly string _versionManifestFileName = "manifest.json";

        public VersionManifestFetcherTests()
        {
            _options = new VersionManifestFetcher.Options()
            {
                DownloadedCacheParentFolder = _fixture.TestLocation.OutputFolder,
                ToolkitContext = _contextFixture.ToolkitContext,
                Name = "sample-server",
                CompatibleMajorVersion = 0
            };
            _sut = new VersionManifestFetcher(_options, _settingsRepository);
            Directory.CreateDirectory(_sut.DownloadedCacheFolder);
        }

        [Fact]
        public async Task Get_WhenDownloadCacheEmptyAndNoCloudFrontAsync()
        {
            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.Null(stream);

            Assert.False(CacheExists());
        }

        [Fact]
        public async Task Get_WhenDownloadCacheNotEmptyAndNoCloudFrontAsync()
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
            _settingsRepository.Settings.LspSettings.VersionManifestFolder = _fixture.TestLocation.InputFolder;

            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_versionManifestSampleData, text);

            // verify that overridden local copy does not get cached
            Assert.False(CacheExists());
        }


        [Fact]
        public async Task Get_WhenValidationFailsWithNoCloudFrontAsync()
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


        [Fact]
        public async Task Get_WhenFetchingFromRemoteLocationWithNoLocalCache()
        {
            Assert.False(CacheExists());
            Assert.Empty(_settingsRepository.Settings.LspSettings.ManifestCachedEtags);

            _options.CloudFrontBaseUrl = $"{CodeWhispererConstants.ManifestBaseCloudFrontUrl}";

            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.NotNull(stream);

            // verify cache is updated and etag cached
            Assert.True(CacheExists());
            Assert.Single(_settingsRepository.Settings.LspSettings.ManifestCachedEtags);
        }

        [Fact]
        public async Task Get_WhenFetchingFromRemoteLocationWithLocalCache()
        {
            _options.CloudFrontBaseUrl = $"{CodeWhispererConstants.ManifestBaseCloudFrontUrl}";

            // setup cache 
            File.WriteAllText(Path.Combine(_sut.DownloadedCacheFolder, _versionManifestFileName),
                _versionManifestSampleData);

            var manifestCachedEtag = new ManifestCachedEtag()
            {
                Etag = "adcd",
                ManifestUrl =
                    $"{_options.CloudFrontBaseUrl}/{_options.CompatibleMajorVersion}/{_versionManifestFileName}"
            };
            _settingsRepository.Settings.LspSettings.ManifestCachedEtags.Add(manifestCachedEtag);

            var stream = await _sut.GetAsync(_versionManifestFileName);
            Assert.NotNull(stream);

            // verify cache is updated and etag cached
            Assert.True(CacheExists());
            var cachedEtags = Enumerable.ToList(_settingsRepository.Settings.LspSettings.ManifestCachedEtags);
            Assert.Single(cachedEtags);
            Assert.NotEqual("abcd", cachedEtags.First().Etag);
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
