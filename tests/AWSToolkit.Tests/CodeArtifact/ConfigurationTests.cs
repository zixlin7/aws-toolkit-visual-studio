using Amazon.AWSToolkit.CodeArtifact.Utils;
using Amazon.AWSToolkit.Tests.Common.IO;
using System;
using System.IO;
using Xunit;

namespace AWSToolkit.Tests.CodeArtifact
{

    public class ConfigurationTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();

        [Fact]
        public void NoFileFound()
        {
            var config = Configuration.LoadConfiguration(Path.GetDirectoryName("not_a_valid_path"));
            Assert.NotNull(config);
            Assert.Null(config.DefaultProfile);
            Assert.Null(config.SourceProfileOverrides);
        }

        [Fact]
        public void LoadConfig()
        {
            string config = "{\"DefaultProfile\":\"my-creds\",\"SourceProfileOverrides\":null}";
            var file = Path.Combine(_testLocation.TestFolder, "config.json");
            File.WriteAllText(file, config);
            var loadedConfig = Configuration.LoadConfiguration(Path.GetFullPath(file));
            Assert.Equal("my-creds", loadedConfig.DefaultProfile);
            Assert.Null(loadedConfig.SourceProfileOverrides);
        }

        [Fact]
        public void MalformedJsonConfig()
        {
            string config = "{\"DefaultProfile\":\"my-creds\",\"SourceProfileOverrides\":null";
            var file = Path.Combine(_testLocation.TestFolder, "config.json");
            File.WriteAllText(file, config);
            Assert.Throws<Exception>(() => Configuration.LoadConfiguration(Path.GetFullPath(file)));
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }
    }
}
