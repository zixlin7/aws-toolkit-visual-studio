using Amazon.AWSToolkit.Publish.Install;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Install
{
    public class InstallOptionsTests
    {
        private readonly string _toolPath = @"my\cool\path";
        private readonly string _versionRange = "0.1.*";
        private readonly InstallOptions _installOptions;

        public InstallOptionsTests()
        {
            _installOptions = new InstallOptions(_toolPath, _versionRange);
        }

        [Fact]
        public void ShouldCreate()
        {
            Assert.Equal(_toolPath, _installOptions.ToolPath);
            Assert.Equal(_versionRange, _installOptions.VersionRange);
        }

        [Fact]
        public void ShouldGetCliInstallPath()
        {
            Assert.Equal(@"my\cool\path\dotnet-aws.exe", _installOptions.GetCliInstallPath());
        }
    }
}
