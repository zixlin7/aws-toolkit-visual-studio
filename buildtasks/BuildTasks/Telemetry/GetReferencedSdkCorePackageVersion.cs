using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;

namespace BuildTasks.Telemetry
{
    /// <summary>
    /// Finds the highest NuGet Package Version of AWSSDK.Core used by the Toolkit
    /// </summary>
    public class GetReferencedSdkCorePackageVersion : BuildTaskBase
    {
        /// <summary>
        /// Top level folder of the AWS Toolkit repo.
        /// All C#/F# project files are searched from within this location.
        /// </summary>
        public string RootLocation { get; set; }

        /// <summary>
        /// Is set to the highest package reference version of AWSSDK.Core found in projects within RootLocation
        /// </summary>
        [Output]
        public string PackageVersion { get; private set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var headVersion = GetProjectFiles(this.RootLocation)
                .SelectMany(GetReferencedPackageVersions)
                .Distinct()
                .Select(versionStr =>
                {
                    if (Version.TryParse(versionStr, out var version))
                    {
                        return version;
                    }

                    Log.LogWarning($"Unable to parse version: {versionStr}");
                    return null;
                })
                .Where(version => version != null)
                .OrderByDescending(version => version)
                .First();

            PackageVersion = headVersion.ToString();

            return true;
        }

        /// <summary>
        /// Searches a project file (eg .csproj) looking for package references to AWSSDK.Core.
        /// Returns all version references found.
        /// </summary>
        private ISet<string> GetReferencedPackageVersions(string projectFile)
        {
            var versions = new HashSet<string>();

            var xdoc = new XmlDocument();
            xdoc.Load(projectFile);

            var packageReferenceNodes =
                xdoc.DocumentElement.SelectNodes(
                    "//*[local-name()='ItemGroup']/*[local-name()='PackageReference'][@Include='AWSSDK.Core']");
            foreach (XmlElement packageReferenceNode in packageReferenceNodes)
            {
                // PackageReference Elements support version declarations as an attribute and as a child element
                var attributeVersion = packageReferenceNode.GetAttribute("Version");
                if (!string.IsNullOrEmpty(attributeVersion))
                {
                    versions.Add(attributeVersion);
                }

                var elementVersion = packageReferenceNode["Version"]?.InnerText;
                if (!string.IsNullOrEmpty(elementVersion))
                {
                    versions.Add(elementVersion);
                }
            }

            if (versions.Count > 1)
            {
                Log.LogWarning(
                    $"Project contains multiple AWSSDK.Core references. File: {projectFile}, Versions: {string.Join(", ", versions)}");
            }

            return versions;
        }

        public static IEnumerable<string> GetProjectFiles(string directory)
        {
            var projectFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".csproj") || s.EndsWith(".fsproj"));
            return projectFiles.Where(x => !x.Contains("buildtasks") && !x.Contains(@"\obj\"));
        }
    }
}
