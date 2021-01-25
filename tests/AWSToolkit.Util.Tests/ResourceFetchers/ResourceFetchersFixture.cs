using System;
using System.IO;
using Amazon.AWSToolkit.Tests.Common.IO;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class ResourceFetchersFixture : IDisposable
    {
        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        public readonly string SampleData = $"Hello world {Guid.NewGuid()}";
        public readonly string SampleRelativePath = "readme.txt";
        public readonly string SampleInputRelativePath = "input/readme.txt";
        public readonly string SampleOutputRelativePath = "output/readme.txt";

        public ResourceFetchersFixture()
        {

        }

        public void WriteToFile(string text, string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, text);
        }

        public string GetFullPath(string relativePath)
        {
            return Path.Combine(TestLocation.TestFolder, relativePath);
        }

        public string GetStreamContents(Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public void Dispose()
        {
            TestLocation.Dispose();
        }
    }
}
