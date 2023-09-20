using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest;

using Newtonsoft.Json;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Manifest
{
    public class ManifestSchemaTests
    {
        private const string _manifestFileName = "sample-manifest.json";

        [Fact]
        public async Task LoadAsync()
        {
            using (var stream = TestResources.LoadResourceFile(_manifestFileName))
            {
                Assert.NotNull(stream);
                var sut = await ManifestSchemaUtil.LoadAsync(stream);

                Assert.NotNull(sut);
                Assert.Equal("0.1", sut.SchemaVersion);
                Assert.Equal(3, sut.Versions.Count);
            }
        }

        [Fact]
        public async Task LoadThrows_WhenInvalidAsync()
        {
            using (var stream = TestResources.LoadResourceFile("sample-invalid-manifest.json"))
            {
                Assert.NotNull(stream);
                var exception =
                    await Assert.ThrowsAsync<JsonSerializationException>(async () =>
                        await ManifestSchemaUtil.LoadAsync(stream));
                Assert.Contains("Required property", exception.Message);
            }
        }
    }
}
