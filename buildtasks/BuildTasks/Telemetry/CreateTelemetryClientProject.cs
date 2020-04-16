using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildTasks.Telemetry
{
    /// <summary>
    /// Takes a Telemetry Client produced from the SDK's Service Generator,
    /// produces a C# Library project that is compatible with the toolkit,
    /// copies generated files and the produced project into the repo.
    /// </summary>
    public class CreateTelemetryClientProject : BuildTaskBase
    {
        private static readonly string TelemetryProjectTemplatePath = Path.Combine(
            Path.GetDirectoryName(typeof(CreateTelemetryClientProject).Assembly.Location),
            "Telemetry",
            "TelemetryProjectTemplate.txt");

        /// <summary>
        /// Folder that contains the generated client project/code
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Generated files to copy into the repo, and reference from the project file
        /// </summary>
        public string IncludeProjectFiles { get; set; }

        /// <summary>
        /// Generated files to refrain from copying, or referencing from the project file.
        /// Takes precedence over IncludeProjectFiles
        /// </summary>
        public string ExcludeProjectFiles { get; set; }

        /// <summary>
        /// NuGet Package Version to use in referencing AWSSDK.Core
        /// </summary>
        public string AwsSdkCoreVersion { get; set; }

        /// <summary>
        /// Name of the the Toolkit's telemetry client project to produce
        /// </summary>
        public string ProjectFilename { get; set; }

        /// <summary>
        /// Folder in the repo to put the generated client project and files into
        /// </summary>
        public string Destination { get; set; }

        public override bool Execute()
        {
            try
            {
                CheckWaitForDebugger();

                // c:\...\SomeRoot\Generated\foo.cs -> Generated\foo.cs
                var relativeSourceFiles = GetSourceProjectFiles()
                    .Select(f => f.Substring(Source.Length + 1))
                    .ToList();

                CopyFiles(relativeSourceFiles);
                WriteProjectFile(relativeSourceFiles);

                return true;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true, true, null);

                return false;
            }
        }

        private void CopyFiles(List<string> relativeSourceFiles)
        {
            relativeSourceFiles.ForEach(CopyToDestination);
        }

        private void WriteProjectFile(List<string> sourceFiles)
        {
            // Produce file include entries, example:
            //    <Compile Include="Generated\AmazonToolkitTelemetryConfig.cs" />
            var projectFiles = string.Join(Environment.NewLine, sourceFiles
                .Select(f => $"    <Compile Include=\"{f}\" />")
            );

            File.WriteAllText(
                Path.Combine(Destination, ProjectFilename),
                CreateTelemetryProject(projectFiles, AwsSdkCoreVersion)
            );
        }

        /// <summary>
        /// Makes a Telemetry Project from the template and returns the file contents
        /// </summary>
        private static string CreateTelemetryProject(string projectFiles, string awsSdkCoreVersion)
        {
            var template = File.ReadAllText(TelemetryProjectTemplatePath);
            template = template.Replace("{PROJECT_FILES}", projectFiles);
            template = template.Replace("{AWSSDK_CORE_VERSION}", awsSdkCoreVersion);

            return template;
        }

        /// <summary>
        /// Copies a file from the Source folder to the Destination folder, preserving its relative location
        /// </summary>
        private void CopyToDestination(string relativeSource)
        {
            var source = Path.Combine(Source, relativeSource);
            var dest = Path.Combine(Destination, relativeSource);

            Directory.CreateDirectory(Path.GetDirectoryName(dest));

            Log.LogMessage($"Copying from {source} to {dest}");
            File.Copy(source, dest, true);
        }

        private IList<string> GetSourceProjectFiles()
        {
            return IncludeProjectFiles.Split(';')
                .Except(ExcludeProjectFiles.Split(';'))
                .ToList();
        }
    }
}
