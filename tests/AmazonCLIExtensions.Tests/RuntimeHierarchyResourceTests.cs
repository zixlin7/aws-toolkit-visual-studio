using System.IO;
using System.Linq;
using Amazon.Lambda.Tools;
using ThirdParty.Json.LitJson;
using Xunit;

namespace AmazonCLIExtensions.Tests
{
    public class RuntimeHierarchyResourceTests
    {
        // this is an internal member of the cli tool
        private const string RuntimeHierarchyResourceName = "netcore.runtime.hierarchy.json";

        private const string ExpectedRuntimeHierarchyResourcePath = "AmazonCLIExtensions.Amazon.Lambda.Tools.Resources.netcore.runtime.hierarchy.json";

        /// <summary>
        /// The full name for the embedded resource changes between the dotnet CLI and AWS Toolkit for VS, test verifies we can find
        /// the file using it's VS path
        /// </summary>
        [Fact]
        public void CanFindResource()
        {
            var manifestName = FindResource(RuntimeHierarchyResourceName);
            Assert.Equal(ExpectedRuntimeHierarchyResourcePath, manifestName);
        }

        [Fact]
        public void ValidateRuntimeHierarchyResourceIsValidJson()
        {
            var manifestName = FindResource(RuntimeHierarchyResourceName);
            using (var stream = typeof(LambdaPackager).Assembly.GetManifestResourceStream(manifestName))
            using (var reader = new StreamReader(stream))
            {
                var rootData = JsonMapper.ToObject(reader.ReadToEnd());
                Assert.NotNull(rootData);
                var runtimes = rootData["runtimes"];
                Assert.NotNull(runtimes);
                Assert.NotEmpty(runtimes);
            }
        }

        private string FindResource(string resourceName)
        {
            var lambdaAssembly = typeof(LambdaPackager).Assembly;
            return lambdaAssembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName));
        }
    }
}
