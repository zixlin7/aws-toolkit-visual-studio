using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class LspManagerTests : IDisposable
    {
        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();

        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();

        private readonly LspManager _sut;
        private readonly ManifestSchema _sampleSchema = new ManifestSchema();
        private readonly LspVersion _sampleLspVersion;
        private readonly string _sampleVersion = $"{CodeWhispererConstants.LspCompatibleVersionRange.Start.Major}.0.2";
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly LspManager.Options _options;


        public LspManagerTests()
        {
            var productEnvironment =
                new ProductEnvironment() { OperatingSystemArchitecture = Architecture.X64.ToString() };
            _contextFixture.ToolkitHost.Setup(x => x.ProductEnvironment)
                .Returns(productEnvironment);

            _options = CreateOptions();
           _sut = new LspManager(_options, _settingsRepository, _sampleSchema);
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
        public async Task DownloadAsync_WhenLocalOverride()
        {
            _settingsRepository.Settings.LanguageServerPath = "test-local-path/abc.exe";
            var path = await _sut.DownloadAsync();

            Assert.Equal(_settingsRepository.Settings.LanguageServerPath, path);
        }

        [Fact]
        public async Task DownloadAsync_WhenManifestSchemaNullAndNoLocalOverride()
        {
            var manager = new LspManager(_options, _settingsRepository, null);
            var exception = await Assert.ThrowsAsync<ToolkitException>(async () => await manager.DownloadAsync());

            Assert.Contains("No valid manifest", exception.Message);
        }


        public static readonly IEnumerable<object[]> IncompatibleManifestVersions = new[]
        {
            new object[] { "0.0.0" }, new object[] { $"{CodeWhispererConstants.LspCompatibleVersionRange.End.Major + 1}.0.2" },
            new object[] { $"{CodeWhispererConstants.LspCompatibleVersionRange.End.Major + 2}.0.2" }
        };

        [Theory]
        [MemberData(nameof(IncompatibleManifestVersions))]
        public async Task DownloadAsync_WhenNoMatchingVersionsFound(string version)
        {
            _sampleLspVersion.Version = version;

            await AssertDownloadAsyncThrowsWithMessage("language server that satisfies one or more of these conditions");
        }

        [Fact]
        public async Task DownloadAsync_WhenNoMatchingPlatform()
        {
            _sampleLspVersion.Targets.First().Platform = "mac";

            // assumption: tests always run in an environment where the underlying OS is windows
            await AssertDownloadAsyncThrowsWithMessage("language server that satisfies one or more of these conditions");
        }


        [Fact]
        public async Task DownloadAsync_WhenNoMatchingFile()
        {
            _sampleLspVersion.Targets.First().Contents.First().Filename = "abc.exe";

            await AssertDownloadAsyncThrowsWithMessage("language server that satisfies one or more of these conditions");
        }


        [Fact]
        public async Task DownloadAsync_WhenVersionCacheAlreadyExists()
        {
            var expectedPath = Path.Combine(_options.DownloadParentFolder, _sampleVersion, CodeWhispererConstants.Filename);
            SetupFile(expectedPath);

            var path = await _sut.DownloadAsync();

            Assert.Equal(expectedPath, path);
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
            await AssertDownloadAsyncThrowsWithMessage("compatible version of");
        }

        [Fact]
        public async Task DownloadAsync_WhenHashesDoNotMatchAndHasFallback()
        {
            // setup target content with required params
            SetupTargetContent("sha384:1234", _sampleLspVersion);

            // setup fallback cache
            var expectedFallbackLocation = Path.Combine(_options.DownloadParentFolder,
                $"{CodeWhispererConstants.LspCompatibleVersionRange.Start.Major}.0.1", CodeWhispererConstants.Filename);
            SetupFile(expectedFallbackLocation);
            var additionalFallbackLocation = Path.Combine(_options.DownloadParentFolder,
                $"{CodeWhispererConstants.LspCompatibleVersionRange.Start.Major}.0.0", CodeWhispererConstants.Filename);
            SetupFile(additionalFallbackLocation);


            var downloadPath = await _sut.DownloadAsync();

            Assert.Equal(expectedFallbackLocation, downloadPath);
        }

        [Fact]
        public async Task DownloadAsync_WhenHashesMatch()
        {
            // setup manifest specified fetch location
            var lspFetchLocation = Path.Combine(TestLocation.InputFolder, _sampleVersion, CodeWhispererConstants.Filename);
            SetupFile(lspFetchLocation);

            var expectedHash = GetHash(lspFetchLocation);

            // setup target content with required params
            SetupTargetContent($"sha384:{expectedHash}", _sampleLspVersion);

            var expectedPath = Path.Combine(_options.DownloadParentFolder, _sampleVersion, CodeWhispererConstants.Filename);
            var downloadPath = await _sut.DownloadAsync();

            Assert.Equal(expectedPath, downloadPath);
        }

        [Fact]
        public async Task DownloadAsync_WhenMultipleVersionsChooseLatest()
        {
            // add additional compatible lsp versions 
            var latestExpectedVersion = $"{CodeWhispererConstants.LspCompatibleVersionRange.Start.Major}.1.1";
            var latestLspVersion = CreateSampleLspVersion(latestExpectedVersion);
            _sampleSchema.Versions.Add(latestLspVersion);

            // setup fetch location
            var lspFetchLocation = Path.Combine(TestLocation.InputFolder, latestExpectedVersion, CodeWhispererConstants.Filename);
            SetupFile(lspFetchLocation);

            var expectedHash = GetHash(lspFetchLocation);

            // setup target content with required params
            SetupTargetContent($"sha384:{expectedHash}", latestLspVersion);

            // verify latest compatible lsp version is chosen amongst multiple available for eg. between 1.1.1 and 1.0.2, 1.1.1 is chosen
            var expectedPath = Path.Combine(_options.DownloadParentFolder, latestExpectedVersion, CodeWhispererConstants.Filename);
            var downloadPath = await _sut.DownloadAsync();

            Assert.Equal(expectedPath, downloadPath);
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
            var version = lspVersion.Version;
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
                VersionRange = CodeWhispererConstants.LspCompatibleVersionRange,
                DownloadParentFolder = TestLocation.OutputFolder
            };
        }

        public static LspVersion CreateSampleLspVersion(string version)
        {
            return new LspVersion()
            {
                Version = version,
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

        private async Task AssertDownloadAsyncThrowsWithMessage(string exceptionMessage)
        {
            var exception = await Assert.ThrowsAsync<ToolkitException>(async () => await _sut.DownloadAsync());

            Assert.Contains(exceptionMessage, exception.Message);
        }


        public void Dispose()
        {
            TestLocation?.Dispose();
            _settingsRepository?.Dispose();
        }
    }
}
