using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.AWSToolkit.Lambda.TemplateWizards;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Util;

using Xunit;

namespace AwsToolkit.Vs.Tests.Lambda.TemplateWizards
{
    public class ProjectCreatorUtilitiesTests : IDisposable
    {
        protected static readonly string[] SampleFiles = new string[]
        {
            "README.md",
            @"src\App.cs",
            @"src\Models\Model.cs",
            @"src/Views/View.cs",
        };

        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();

        public ProjectCreatorUtilitiesTests()
        {
            SampleFiles.ToList().ForEach(path => { PlaceholderFile.Create(Path.Combine(_testLocation.InputFolder, path)); });
        }

        [Fact]
        public void CreateFromZipFile()
        {
            // Make a sample zip file
            var replacements = new Dictionary<string, string>()
            {
                {"$destinationdirectory$", _testLocation.TestFolder}
            };
            var zipFile = Path.Combine(_testLocation.TestFolder, "test.zip");
            ZipUtil.CreateZip(zipFile, _testLocation.InputFolder);

            // Try extracting the zip file
            var relativeOutputPath = Path.GetFileName(_testLocation.OutputFolder);
            ProjectCreatorUtilities.CreateFromZipFile(replacements, zipFile, relativeOutputPath);

            // Verify the extraction
            var srcFiles = Directory.GetFiles(_testLocation.InputFolder, "*.*", SearchOption.AllDirectories)
                .Select(file => file.Substring(_testLocation.InputFolder.Length))
                .ToList();

            var dstFiles = Directory.GetFiles(_testLocation.OutputFolder, "*.*", SearchOption.AllDirectories)
                .Select(file => file.Substring(_testLocation.OutputFolder.Length))
                .ToList();

            Assert.NotEmpty(dstFiles);
            Assert.Equal(srcFiles, dstFiles);
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }
    }
}
