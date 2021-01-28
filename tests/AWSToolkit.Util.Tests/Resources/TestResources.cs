using System.IO;

namespace Amazon.AWSToolkit.Util.Tests.Resources
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

        public static string LoadResourceFileText(string fileName)
        {
            using (var stream = LoadResourceFile(fileName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
