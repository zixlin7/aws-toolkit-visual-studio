using System;
using System.IO;

namespace Amazon.AWSToolkit.Tests.Common.IO
{
    public static class PlaceholderFile
    {
        /// <summary>
        /// Produces a file with filler content.
        /// Useful for tests that need a file but don't care about its contents.
        /// 
        /// Parent directories are created if necessary.
        /// </summary>
        public static void Create(string filename)
        {
            var parentFolder = Path.GetDirectoryName(filename);

            if (parentFolder == null)
            {
                throw new Exception($"{nameof(filename)} did not have a parent folder");
            }

            if (!Directory.Exists(parentFolder))
            {
                Directory.CreateDirectory(parentFolder);
            }

            if (File.Exists(filename))
            {
                throw new Exception($"File already exists: {filename}");
            }

            using (var fs = File.OpenWrite(filename))
            using (var w = new StreamWriter(fs))
            {
                w.WriteLine("Sample test file");
                w.WriteLine(filename);
                w.WriteLine(Guid.NewGuid());
            }
        }
    }
}