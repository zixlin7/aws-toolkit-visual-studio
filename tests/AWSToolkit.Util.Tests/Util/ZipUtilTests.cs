using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.Tests.Common.IO;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Util
{
    public class ZipUtilTests : IDisposable
    {
        protected static readonly string[] SampleFiles = new string[]
        {
            "hi.txt",
            "README.md",
            @"bin\App.exe",
            @"src\App.cs",
            @"src\Models\Model.cs",
            @"src/Views/View.cs",
        };

        protected readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        protected readonly string ZipFile;

        public ZipUtilTests()
        {
            ZipFile = Path.Combine(TestLocation.TestFolder, "test.zip");

            SampleFiles.ToList().ForEach(path => { PlaceholderFile.Create(Path.Combine(TestLocation.InputFolder, path)); });
        }

        public void Dispose()
        {
            TestLocation.Dispose();
        }
    }

    public class CreateZipTests : ZipUtilTests
    {
        [Fact]
        public void ZipFileAlreadyExists()
        {
            PlaceholderFile.Create(ZipFile);

            Assert.Throws<Exception>(() =>
            {
                ZipUtil.CreateZip(ZipFile, new Dictionary<string, string>());
            });

            Assert.Throws<Exception>(() =>
            {
                ZipUtil.CreateZip(ZipFile, TestLocation.TestFolder);
            });
        }

        [Fact]
        public void CreatesZipWithFiles()
        {
            var contents = SampleFiles
                .ToDictionary(
                    file => Path.Combine(TestLocation.InputFolder, file),
                    file => file
                );

            ZipUtil.CreateZip(ZipFile, contents);

            Assert.True(File.Exists(ZipFile));

            AssertZipContents(WithForwardSlashes(contents.Values));

            System.IO.Compression.ZipFile.ExtractToDirectory(ZipFile, TestLocation.OutputFolder);

            AssertOutputFolderContents(WithBackSlashes(contents.Values));
        }

        [Fact]
        public void CreatesZipWithFolder()
        {
            var contents = SampleFiles
                .ToDictionary(
                    file => Path.Combine(TestLocation.InputFolder, file),
                    file => file
                );

            ZipUtil.CreateZip(ZipFile, TestLocation.InputFolder);

            Assert.True(File.Exists(ZipFile));

            AssertZipContents(WithForwardSlashes(contents.Values));

            System.IO.Compression.ZipFile.ExtractToDirectory(ZipFile, TestLocation.OutputFolder);

            AssertOutputFolderContents(WithBackSlashes(contents.Values));
        }

        private void AssertZipContents(string[] expectedFilePaths)
        {
            using (var zipArchive = System.IO.Compression.ZipFile.OpenRead(ZipFile))
            {
                Assert.Equal(expectedFilePaths.Length, zipArchive.Entries.Count);

                var zippedFilePaths = zipArchive.Entries
                    .Select(entry => entry.FullName)
                    .ToArray();

                Assert.Equal(expectedFilePaths, zippedFilePaths);
            }
        }

        private void AssertOutputFolderContents(string[] expectedRelativeFilePaths)
        {
            var expectedFiles = expectedRelativeFilePaths.Select(file => Path.Combine(TestLocation.OutputFolder, file)).ToArray();
            var actualFiles = Directory.GetFiles(TestLocation.OutputFolder, "*.*", SearchOption.AllDirectories);
            Assert.Equal(expectedFiles, actualFiles);
        }

        private string[] WithBackSlashes(IEnumerable<string> paths)
        {
            return paths
                .Select(path => path.Replace('/', '\\'))
                .ToArray();
        }

        private string[] WithForwardSlashes(IEnumerable<string> paths)
        {
            return paths
                .Select(path => path.Replace('\\', '/'))
                .ToArray();
        }
    }

    public class ExtractZipTests : ZipUtilTests
    {
        public ExtractZipTests()
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(TestLocation.InputFolder, ZipFile);
        }

        [Fact]
        public void ZipFileDoesNotExist()
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                ZipUtil.ExtractZip(Path.Combine(TestLocation.TestFolder, "fake.zip"), TestLocation.TestFolder, true);
            });
        }

        [Fact]
        public void ExtractsFiles()
        {
            ZipUtil.ExtractZip(ZipFile, TestLocation.OutputFolder, true);

            SampleFiles
                .Select(file => Path.Combine(TestLocation.OutputFolder, file))
                .ToList()
                .ForEach(expectedFile => Assert.True(File.Exists(expectedFile)));
        }

        [Fact]
        public void OverwritesFiles()
        {
            ZipUtil.ExtractZip(ZipFile, TestLocation.OutputFolder, true);

            // This should not throw
            ZipUtil.ExtractZip(ZipFile, TestLocation.OutputFolder, true);
        }

        [Fact]
        public void DoesNotOverwriteFiles()
        {
            ZipUtil.ExtractZip(ZipFile, TestLocation.OutputFolder, true);
            Assert.ThrowsAny<Exception>(() =>
            {
                ZipUtil.ExtractZip(ZipFile, TestLocation.OutputFolder, false);
            });
        }
    }
}