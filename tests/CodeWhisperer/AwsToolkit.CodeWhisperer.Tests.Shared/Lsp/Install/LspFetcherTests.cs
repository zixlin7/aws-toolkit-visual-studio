using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AWSToolkit.Util.Tests.ResourceFetchers;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class LspFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly LspFetcher.Options _options;
        private readonly LspFetcher _sut;
        private readonly string _sampleVersion = "0.0.0";
        private readonly string _sampleLspFilename = "test-lsp-binary.exe";

        public LspFetcherTests()
        {
            var downloadFolder = Path.Combine(_fixture.TestLocation.OutputFolder, _sampleVersion);
            _options = new LspFetcher.Options()
            {
                DownloadedCachePath = Path.Combine(downloadFolder, _sampleLspFilename),
                Version = _sampleVersion,
                Filename = _sampleLspFilename,
                TempCacheFolderPath = _fixture.TestLocation.TestFolder
            };
            _sut = new LspFetcher(_options);
        }

        [Fact]
        public async Task Get_WhenInvalidPath()
        {
            var path = Path.Combine(_fixture.TestLocation.InputFolder, "abc.exe");
            var stream = await _sut.GetAsync(path);
            Assert.Null(stream);

            Assert.False(CacheExists());
        }

        [Fact]
        public async Task Get_WhenValidPath()
        {
            var path = Path.Combine(_fixture.TestLocation.InputFolder, _sampleLspFilename);
            File.WriteAllText(path, "hello");

            var stream = await _sut.GetAsync(path);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal("hello", text);

            Assert.True(CacheExists());
            Assert.False(TempValidationCacheExists());
        }

        [Fact]
        public async Task Get_WhenResourceValidationFails()
        {
            _options.ResourceValidator = s => Task.FromResult(false);

            var path = Path.Combine(_fixture.TestLocation.InputFolder, _sampleLspFilename);
            File.WriteAllText(path, "hello");


            var stream = await _sut.GetAsync(path);
            Assert.Null(stream);

            Assert.False(CacheExists());
            Assert.False(TempValidationCacheExists());
        }

        public void Dispose()
        {
            _fixture?.Dispose();
        }

        private bool CacheExists()
        {
            return File.Exists(_options.DownloadedCachePath);
        }

        private bool TempValidationCacheExists()
        {
            return File.Exists(_sut.TempCachePath);
        }
    }
}
