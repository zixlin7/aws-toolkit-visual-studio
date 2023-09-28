using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class LspInstallUtilTests : IDisposable
    {
        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();

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
                Path.Combine(TestLocation.TestFolder, $"{LspConstants.LspCompatibleVersionRange.Start.Major}.1.1");

            var fallbackPath = LspInstallUtil.GetFallbackVersionFolder(TestLocation.TestFolder,
                $"{LspConstants.LspCompatibleVersionRange.Start.Major}.2.0");
            Assert.Equal(expectedFallbackPath, fallbackPath);
        }


        [Fact]
        public void GetFallbackVersionFolder_WhenNoMatchingVersions()
        {
            CreateDirectoryForVersion(LspConstants.LspCompatibleVersionRange.End.Major + 1);
            var fallbackPath = LspInstallUtil.GetFallbackVersionFolder(TestLocation.TestFolder,
                $"{LspConstants.LspCompatibleVersionRange.Start.Major}.2.0");
            Assert.Null(fallbackPath);
        }

        private void SetupDirectories()
        {
            Enumerable.Range(LspConstants.LspCompatibleVersionRange.Start.Major - 1, 3)
                .ToList().ForEach(CreateDirectoryForVersion);
        }

        private void CreateDirectoryForVersion(int version)
        {
            var path = Path.Combine(TestLocation.TestFolder, $"{version}.1.1");
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
