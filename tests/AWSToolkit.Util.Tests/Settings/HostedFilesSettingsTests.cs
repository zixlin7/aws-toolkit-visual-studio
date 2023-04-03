using System;
using System.IO;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class HostedFilesSettingsTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly ToolkitSettings _toolkitSettings = FakeToolkitSettings.Create();
        private readonly string _downloadCacheFolder;
        private readonly HostedFilesSettings _sut;

        public HostedFilesSettingsTests()
        {
            _downloadCacheFolder = _testLocation.InputFolder;
            _sut = new HostedFilesSettings(_toolkitSettings, _downloadCacheFolder);
        }

        [Fact]
        public void DownloadedCacheFolder()
        {
            Assert.Equal(Path.Combine(_downloadCacheFolder, "downloadedfiles"), _sut.DownloadedCacheFolder);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HostedFilesLocationAsUri_Unset(string hostedFilesLocation)
        {
            _toolkitSettings.HostedFilesLocation = hostedFilesLocation;
            Assert.Null(_sut.HostedFilesLocationAsUri);
        }

        [Theory]
        [InlineData(S3FileFetcher.HOSTEDFILES_LOCATION)]
        public void HostedFilesLocationAsUri_AsUrl(string hostedFilesLocation)
        {
            _toolkitSettings.HostedFilesLocation = hostedFilesLocation;
            Assert.NotNull(_sut.HostedFilesLocationAsUri);
            Assert.StartsWith("http", _sut.HostedFilesLocationAsUri.Scheme);
            Assert.Equal(hostedFilesLocation, _sut.HostedFilesLocationAsUri.ToString());
        }

        [Theory]
        [InlineData("file://c:/temp/readme.txt")]
        [InlineData(@"file://c:\temp\readme.txt")]
        [InlineData("c:/temp/readme.txt")]
        [InlineData(@"c:\temp\readme.txt")]
        public void HostedFilesLocationAsUri_AsFile(string path)
        {
            _toolkitSettings.HostedFilesLocation = path;
            Assert.NotNull(_sut.HostedFilesLocationAsUri);
            Assert.True(_sut.HostedFilesLocationAsUri.IsFile);
            Assert.Equal("file:///c:/temp/readme.txt", _sut.HostedFilesLocationAsUri.ToString());
        }

        [Fact]
        public void HostedFilesLocationAsUri_AsRegion()
        {
            _toolkitSettings.HostedFilesLocation = "region://us-west-2";
            Assert.NotNull(_sut.HostedFilesLocationAsUri);
            Assert.StartsWith("https://aws-vs-toolkit-us-west-2.", _sut.HostedFilesLocationAsUri.ToString());
        }

        public void Dispose()
        {
            _testLocation?.Dispose();
        }
    }
}
