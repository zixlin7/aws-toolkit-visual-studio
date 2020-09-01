using System;
using System.IO;

namespace BuildTasks.Test.IO
{
    /// <summary>
    /// Class represents a temporary test folder to be used as repository root with changelog tests
    /// </summary>
    public class TemporaryTestLocation : IDisposable
    {
        public readonly string TestFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public TemporaryTestLocation()
        {
            Directory.CreateDirectory(TestFolder);
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