using System.IO;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Manifest
{
    internal static class TestResources
    {
        public static Stream LoadResourceFile(string fileName)
        {
            var type = typeof(TestResources);
            var ns = type.Namespace;
            var stream = type.Assembly.GetManifestResourceStream($"{ns}.{fileName}");
            return stream;
        }
    }
}
