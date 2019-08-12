using System.IO;

namespace Amazon.AWSToolkit.Util.Tests
{
    public static class TestUtil
    {
        public static Stream LoadInvalidXmlEndpointsFile(string fileName)
        {
            var stream = typeof(TestUtil).Assembly.GetManifestResourceStream("Amazon.AWSToolkit.Util.Tests.Resources." + fileName);
            return stream;
        }

    }
}
