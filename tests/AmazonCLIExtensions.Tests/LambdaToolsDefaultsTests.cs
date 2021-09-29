using System;
using System.IO;

using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Lambda.Tools;

using Xunit;

namespace AmazonCLIExtensions.Tests
{
    public class LambdaToolsDefaultsTests : IDisposable
    {
        private readonly string _jsonFilename = "settings.json";
        private readonly string _sampleJsonContents = @"
{
    ""function-architecture"": ""x86_64""
}";

        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();

        [Fact]
        public void LoadDefaults()
        {
            WriteSampleContents();

            var lambdaToolsDefaults = new LambdaToolsDefaults();
            lambdaToolsDefaults.LoadDefaults(_testLocation.TestFolder, _jsonFilename);

            Assert.Equal("x86_64", lambdaToolsDefaults.FunctionArchitecture);
        }

        public void Dispose()
        {
            _testLocation?.Dispose();
        }

        private void WriteSampleContents()
        {
            var path = Path.Combine(_testLocation.TestFolder, _jsonFilename);
            File.WriteAllText(path, _sampleJsonContents);
        }
    }
}
