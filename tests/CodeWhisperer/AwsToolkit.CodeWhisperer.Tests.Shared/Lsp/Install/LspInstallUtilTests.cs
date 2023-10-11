using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

using Directory = System.IO.Directory;
using Path = System.IO.Path;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class LspInstallUtilTests : IDisposable
    {
        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        private readonly string _lspVersion = "2.2.0";
        private readonly int _majorLspVersion = 2;

        private readonly IList<Version> _validVersions =
            new List<Version>()
            {
                new Version("2.0.0"), new Version("2.1.1"), new Version("2.1.9"), new Version("2.2.2")
            };

        public static readonly IEnumerable<object[]> ArchitectureData = new[]
        {
            new object[] { Architecture.X64.ToString(), Architecture.Arm64.ToString(), false },
            new object[] { Architecture.Arm64.ToString(), Architecture.Arm64.ToString(), true },
            new object[] { Architecture.Arm64.ToString(), Architecture.X64.ToString(), false },
            new object[] { Architecture.X64.ToString(), Architecture.X64.ToString(), true }
        };

        [Theory]
        [MemberData(nameof(ArchitectureData))]
        public void HasMatchingArchitecture(string systemArch, string lspArchitecture, bool expectedResult)
        {
            var result = LspInstallUtil.HasMatchingArchitecture(systemArch, lspArchitecture);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("mac", false)]
        [InlineData("linux", false)]
        [InlineData("foo", false)]
        [InlineData("unknown", false)]
        [InlineData("windows", true)]
        [InlineData("WINDOWS", true)]
        public void HasMatchingPlatform(string lspPlatform, bool expectedResult)
        {
            var result = LspInstallUtil.HasMatchingPlatform(lspPlatform);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetFallbackVersionFolder()
        {
            SetupDirectories();
            var expectedFallbackPath =
                Path.Combine(TestLocation.TestFolder, $"{_majorLspVersion}.1.1");

            var fallbackPath =
                LspInstallUtil.GetFallbackVersionFolder(TestLocation.TestFolder, _lspVersion, _validVersions);
            Assert.Equal(expectedFallbackPath, fallbackPath);
        }


        [Fact]
        public void GetFallbackVersionFolder_WhenNoMatchingVersions()
        {
            CreateDirectoryForVersion(_majorLspVersion + 1);
            var fallbackPath =
                LspInstallUtil.GetFallbackVersionFolder(TestLocation.TestFolder, _lspVersion, _validVersions);
            Assert.Null(fallbackPath);
        }


        [Fact]
        public void GetAllCachedVersions()
        {
            CreateDirectoryForVersion(1);
            CreateDirectoryForVersion(2);
            //setup non-version pattern directory
            CreateDirectory(Path.Combine(TestLocation.TestFolder, "sample-directory"));

            var expectedVersions = new List<string> { "1.1.1", "2.1.1" };

            var versions = LspInstallUtil.GetAllCachedVersions(TestLocation.TestFolder).Select(x => x.ToString());

            Assert.Equal(expectedVersions, versions);
        }

        private void SetupDirectories()
        {
            Enumerable.Range(_majorLspVersion - 1, 3)
                .ToList().ForEach(CreateDirectoryForVersion);
        }

        private void CreateDirectoryForVersion(int version)
        {
            var path = Path.Combine(TestLocation.TestFolder, $"{version}.1.1");
            CreateDirectory(path);
        }

        private void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Dispose()
        {
            TestLocation?.Dispose();
        }
    }
}
