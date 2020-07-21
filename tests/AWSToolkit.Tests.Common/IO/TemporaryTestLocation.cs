using System;
using System.IO;

namespace Amazon.AWSToolkit.Tests.Common.IO
{
    public class TemporaryTestLocation : IDisposable
    {
        public readonly string TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        public readonly string InputFolder;
        public readonly string OutputFolder;

        public TemporaryTestLocation()
        {
            Directory.CreateDirectory(TestFolder);

            InputFolder = Path.Combine(TestFolder, "input");
            Directory.CreateDirectory(InputFolder);

            OutputFolder = Path.Combine(TestFolder, "output");
            Directory.CreateDirectory(OutputFolder);
        }

        public void Dispose()
        {
            if (Directory.Exists(TestFolder))
            {
                Directory.Delete(TestFolder, true);
            }
        }
    }
}