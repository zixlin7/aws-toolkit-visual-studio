using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit;
using Amazon.AwsToolkit.CodeWhisperer.Lsp;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Util;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class LspManagerTests : IDisposable
    {
        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        private readonly LspManager _sut;
        private readonly ManifestSchema _sampleSchema = new ManifestSchema();
        private readonly LspVersion _sampleLspVersion;
        private static readonly VersionRange _sampleVersionRange = VersionRangeUtil.Create("1.0.0", "2.0.0");
        private readonly string _sampleVersion = $"{_sampleVersionRange.Start.Major}.0.2";

        private readonly string _additionalVersion = $"{_sampleVersionRange.Start.Major}.0.1";

        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly LspManager.Options _options;


        public LspManagerTests()
        {
            var productEnvironment =
                new ProductEnvironment() { OperatingSystemArchitecture = Architecture.X64.ToString() };
            _contextFixture.ToolkitHost.Setup(x => x.ProductEnvironment)
                .Returns(productEnvironment);

            _options = CreateOptions();
            _sut = new LspManager(_options, _sampleSchema);
            Directory.CreateDirectory(_options.DownloadParentFolder);
            _sampleLspVersion = CreateSampleLspVersion(_sampleVersion);
            _sampleSchema.Versions = new List<LspVersion>() { _sampleLspVersion };
        }

        [Fact]
        public async Task DownloadAsync_WhenCancelled()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _sut.DownloadAsync(tokenSource.Token));
        }

        [Fact]
        public async Task DownloadAsync_WhenManifestSchemaNull()
        {
            var manager = new LspManager(_options, null);
            var exception = await Assert.ThrowsAsync<LspToolkitException>(async () => await manager.DownloadAsync());

            Assert.Contains("No valid manifest", exception.Message);
            Assert.Equal(LspToolkitException.LspErrorCode.UnexpectedManifestError.ToString(), exception.Code);
        }


        public static readonly IEnumerable<object[]> IncompatibleManifestVersions = new[]
        {
            new object[] { $"{_sampleVersionRange.Start.Major - 1}.0.2" },
            new object[] { $"{_sampleVersionRange.End.Major + 1}.0.2" },
            new object[] { $"{_sampleVersionRange.End.Major + 2}.0.2" }
        };

        [Theory]
        [MemberData(nameof(IncompatibleManifestVersions))]
        public async Task DownloadAsync_WhenNoMatchingVersionsFound(string version)
        {
            _sampleLspVersion.ServerVersion = version;

            await AssertDownloadAsyncThrowsWithMessage(
                "language server that satisfies one or more of these conditions", LspToolkitException.LspErrorCode.NoCompatibleLspVersion);
        }

        [Fact]
        public async Task DownloadAsync_WhenNoMatchingPlatform()
        {
            _sampleLspVersion.Targets.First().Platform = "mac";

            // assumption: tests always run in an environment where the underlying OS is windows
            await AssertDownloadAsyncThrowsWithMessage(
                "language server that satisfies one or more of these conditions", LspToolkitException.LspErrorCode.NoCompatibleLspVersion);
        }


        [Fact]
        public async Task DownloadAsync_WhenNoMatchingFile()
        {
            _sampleLspVersion.Targets.First().Contents.First().Filename = "abc.exe";

            await AssertDownloadAsyncThrowsWithMessage(
                "language server that satisfies one or more of these conditions", LspToolkitException.LspErrorCode.NoCompatibleLspVersion);
        }


        [Fact]
        public async Task DownloadAsync_WhenValidVersionCacheAlreadyExists()
        {
            var expectedPath = Path.Combine(_options.DownloadParentFolder, _sampleVersion,
                CodeWhispererConstants.Filename);
            SetupFile(expectedPath);
            var hash = GetHash(expectedPath);
            // setup target content with required params
            SetupTargetContent($"sha384:{hash}", _sampleLspVersion);

            var result = await _sut.DownloadAsync();

            Assert.Equal(expectedPath, result.Path);
            Assert.Equal(LanguageServerLocation.Cache, result.Location);
        }

        [Theory]
        [InlineData("sha384:1234")]
        [InlineData("md5:12345")]
        [InlineData("abc")]
        [InlineData("")]
        public async Task DownloadAsync_WhenHashesDoNotMatchAndNoFallback(string hashString)
        {
            // setup target content with required params
            SetupTargetContent(hashString, _sampleLspVersion);
            await AssertDownloadAsyncThrowsWithMessage("compatible version of", LspToolkitException.LspErrorCode.NoValidLspFallback);
        }

        [Fact]
        public async Task DownloadAsync_WhenHashesDoNotMatchAndHasValidFallback()
        {
            // setup compatible target content with invalid hash
            SetupTargetContent("sha384:1234", _sampleLspVersion);

            // setup manifest schema with fallback version
            var fallbackVersion = CreateSampleLspVersion(_additionalVersion);
            _sampleSchema.Versions.Add(fallbackVersion);


            // setup fallback cache
            var expectedFallbackLocation = SetupFileInCache(_additionalVersion);
            var additionalFallbackLocation =
                SetupFileInCache($"{_sampleVersionRange.Start.Major}.0.0");
            // setup fallback target content with required params
            var hash = GetHash(expectedFallbackLocation);
            SetupTargetContent($"sha384:{hash}", fallbackVersion);

            var result = await _sut.DownloadAsync();

            Assert.Equal(expectedFallbackLocation, result.Path);
            Assert.Equal(LanguageServerLocation.Fallback, result.Location);
        }

        [Fact]
        public async Task DownloadAsync_WhenHashesDoNotMatchAndHasInvalidFallback()
        {
            // setup compatible target content with invalid hash
            SetupTargetContent("sha384:1234", _sampleLspVersion);

            // setup manifest schema with fallback version
            var fallbackVersion = CreateSampleLspVersion(_additionalVersion);
            _sampleSchema.Versions.Add(fallbackVersion);


            // setup fallback cache
            var invalidFallbackLocation = SetupFileInCache(_additionalVersion);
            var additionalFallbackLocation =
                SetupFileInCache($"{_sampleVersionRange.Start.Major}.0.0");
            // setup fallback target content with incompatible params
            SetupTargetContent("sha384:5688", fallbackVersion);

            await AssertDownloadAsyncThrowsWithMessage("compatible version of", LspToolkitException.LspErrorCode.NoValidLspFallback);
        }

        [Fact]
        public async Task DownloadAsync_WhenHashesDoNotMatchAndDeListedFallback()
        {
            // setup target content with required params
            SetupTargetContent("sha384:1234", _sampleLspVersion);

            // setup manifest schema with de-listed fallback version
            var deListedLspVersion = CreateSampleLspVersion(_additionalVersion);
            deListedLspVersion.IsDelisted = true;
            _sampleSchema.Versions.Add(deListedLspVersion);


            // setup fallback cache
            var deListedFallbackLocation = SetupFileInCache(_additionalVersion);
            var additionalFallbackLocation =
                SetupFileInCache($"{_sampleVersionRange.Start.Major}.0.0");

            await AssertDownloadAsyncThrowsWithMessage("compatible version of", LspToolkitException.LspErrorCode.NoValidLspFallback);
        }

        [Fact]
        public async Task DownloadAsync_WhenHashesMatch()
        {
            // setup manifest specified fetch location
            var lspFetchLocation =
                Path.Combine(TestLocation.InputFolder, _sampleVersion, CodeWhispererConstants.Filename);
            SetupFile(lspFetchLocation);

            var expectedHash = GetHash(lspFetchLocation);

            // setup target content with required params
            SetupTargetContent($"sha384:{expectedHash}", _sampleLspVersion);

            var expectedPath = Path.Combine(_options.DownloadParentFolder, _sampleVersion,
                CodeWhispererConstants.Filename);
            var result = await _sut.DownloadAsync();

            Assert.Equal(expectedPath, result.Path);
            Assert.Equal(LanguageServerLocation.Remote, result.Location);
        }

        [Fact]
        public async Task DownloadAsync_WhenMultipleVersionsChooseLatest()
        {
            // add additional compatible lsp versions 
            var latestExpectedVersion = $"{_sampleVersionRange.Start.Major}.1.1";
            var latestLspVersion = CreateSampleLspVersion(latestExpectedVersion);
            _sampleSchema.Versions.Add(latestLspVersion);

            // setup fetch location
            var lspFetchLocation = Path.Combine(TestLocation.InputFolder, latestExpectedVersion,
                CodeWhispererConstants.Filename);
            SetupFile(lspFetchLocation);

            var expectedHash = GetHash(lspFetchLocation);

            // setup target content with required params
            SetupTargetContent($"sha384:{expectedHash}", latestLspVersion);

            // verify latest compatible lsp version is chosen amongst multiple available for eg. between 1.1.1 and 1.0.2, 1.1.1 is chosen
            var expectedPath = Path.Combine(_options.DownloadParentFolder, latestExpectedVersion,
                CodeWhispererConstants.Filename);
            var result = await _sut.DownloadAsync();

            Assert.Equal(expectedPath, result.Path);
            Assert.Equal(LanguageServerLocation.Remote, result.Location);
        }

        [Fact]
        public async Task CleanupAsync_WhenInvalidSchemaFails()
        {
            // setup cache path
            var cachePath = SetupFileInCache(_sampleVersion);

            _sampleSchema.Versions = new List<LspVersion>();
            await _sut.CleanupAsync();

            // verify no cleanup occurs when no valid manifest versions
            Assert.True(File.Exists(cachePath));
        }

        [Fact]
        public async Task CleanupAsync_WhenDeListedVersion()
        {
            // setup valid version in cache
            var validCachePath = SetupFileInCache(_sampleVersion);

            // setup de-listed version in cache
            var deListedVersion = CreateSampleLspVersion(_additionalVersion);
            deListedVersion.IsDelisted = true;
            _sampleSchema.Versions.Add(deListedVersion);
            var deListedPath = SetupFileInCache(deListedVersion.ServerVersion);

            await _sut.CleanupAsync();

            // verify de-listed version is deleted
            Assert.False(File.Exists(deListedPath));
            Assert.True(File.Exists(validCachePath));
        }

        [Fact]
        public async Task CleanupAsync_WhenExtraVersions()
        {
            // setup cache with extra versions
            var extraVersion1 =
                CreateSampleLspVersion($"{_sampleVersionRange.Start.Major}.0.1");
            var extraVersion2 =
                CreateSampleLspVersion($"{_sampleVersionRange.Start.Major}.0.3");
            _sampleSchema.Versions.Add(extraVersion1);
            _sampleSchema.Versions.Add(extraVersion2);

            var versionPath = SetupFileInCache(_sampleVersion);
            var versionPath1 = SetupFileInCache(extraVersion1.ServerVersion);
            var versionPath2 = SetupFileInCache(extraVersion2.ServerVersion);

            await _sut.CleanupAsync();

            // verify lowest version is deleted
            Assert.False(File.Exists(versionPath1));
            Assert.True(File.Exists(versionPath));
            Assert.True(File.Exists(versionPath2));
        }

        private string SetupFileInCache(string version)
        {
            var path =
                Path.Combine(_options.DownloadParentFolder, version, CodeWhispererConstants.Filename);
            SetupFile(path);
            return path;
        }


        private string GetHash(string lspFetchLocation)
        {
            using (var stream = File.OpenRead(lspFetchLocation))
            {
                return LspInstallUtil.GetHash(stream);
            }
        }

        private static void SetupFile(string path)
        {
            var file = new FileInfo(path);
            if (!Directory.Exists(file.Directory.FullName))
            {
                Directory.CreateDirectory(file.Directory.FullName);
            }

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "hello");
            }
        }

        private void SetupTargetContent(string hashString, LspVersion lspVersion)
        {
            var version = lspVersion.ServerVersion;
            var lspFetchLocation = Path.Combine(TestLocation.InputFolder, version, CodeWhispererConstants.Filename);
            SetupFile(lspFetchLocation);

            var content = lspVersion.Targets.First().Contents.First();
            content.Hashes = new List<string>() { hashString };
            content.Url = lspFetchLocation;
        }

        private LspManager.Options CreateOptions()
        {
            return new LspManager.Options()
            {
                Filename = CodeWhispererConstants.Filename,
                ToolkitContext = _contextFixture.ToolkitContext,
                VersionRange = _sampleVersionRange,
                DownloadParentFolder = TestLocation.OutputFolder
            };
        }

        public static LspVersion CreateSampleLspVersion(string version)
        {
            return new LspVersion()
            {
                ServerVersion = version,
                Targets = new List<VersionTarget>()
                {
                    CreateVersionTarget(Architecture.X64.ToString(), OSPlatform.Windows.ToString(),
                        CodeWhispererConstants.Filename)
                }
            };
        }

        private static VersionTarget CreateVersionTarget(string arch, string platform, string filename)
        {
            return new VersionTarget()
            {
                Arch = arch,
                Platform = platform,
                Contents = new List<TargetContent>() { new TargetContent() { Filename = filename } }
            };
        }

        private async Task AssertDownloadAsyncThrowsWithMessage(string exceptionMessage, LspToolkitException.LspErrorCode errorCode)
        {
            var exception = await Assert.ThrowsAsync<LspToolkitException>(async () => await _sut.DownloadAsync());

            Assert.Equal(errorCode.ToString(), exception.Code);
            Assert.Contains(exceptionMessage, exception.Message);
        }


        public void Dispose()
        {
            TestLocation?.Dispose();
        }
    }
}
