using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace BuildTasks.Telemetry
{
    public class UpdateTelemetryReferencesTask : BuildTaskBase
    {
        private const int TimeoutMs = 10000;
        private const string NugetUrl = "https://api.nuget.org/v3/index.json";
        private static readonly string[] TelemetryPackageNames = {
            "AWS.Toolkit.Telemetry.Events",
            "AWS.Toolkit.Telemetry.SDK",
        };

        public string RootLocation { get; set; }

        private readonly Dictionary<string, string> _packageToMaxVersion = new Dictionary<string, string>();

        public override bool Execute()
        {
            CheckWaitForDebugger();

            LookupLatestPackageVersions().Wait(TimeoutMs);

            Log.LogMessage($"Searching for projects starting at root {this.RootLocation}");
            var projectFiles = GetProjectFiles(this.RootLocation);
            foreach (var projectFile in projectFiles)
            {
                Log.LogMessage($"Processing {projectFile}");
                var xdoc = new XmlDocument();
                xdoc.Load(projectFile);

                var changed = false;
                var packageReferenceNodes = xdoc.DocumentElement
                    .SelectNodes("//*[local-name()='ItemGroup']/*[local-name()='PackageReference']");
                foreach (XmlElement packageReferenceNode in packageReferenceNodes)
                {
                    var packageId = packageReferenceNode.GetAttribute("Include");
                    if (string.IsNullOrEmpty(packageId) || !_packageToMaxVersion.ContainsKey(packageId))
                    {
                        continue;
                    }

                    var latestVersion = _packageToMaxVersion[packageId];

                    var projectVersion = packageReferenceNode.GetAttribute("Version");
                    if (!string.IsNullOrEmpty(projectVersion))
                    {
                        if (string.Equals(latestVersion, projectVersion))
                        {
                            continue;
                        }

                        Log.LogMessage($"\tUpdated {packageId}: {projectVersion} -> {latestVersion}");
                        changed = true;
                        packageReferenceNode.SetAttribute("Version", latestVersion);
                    }
                    else if (!string.IsNullOrEmpty((projectVersion = packageReferenceNode["Version"]?.InnerText)))
                    {
                        if (string.Equals(latestVersion, projectVersion))
                        {
                            continue;
                        }

                        Console.WriteLine($"\tUpdated {packageId}: {projectVersion} -> {latestVersion}");
                        changed = true;
                        packageReferenceNode["Version"].InnerText = latestVersion;
                    }
                }

                if (changed)
                {
                    xdoc.Save(projectFile);
                }
            }

            return true;
        }

        private async Task LookupLatestPackageVersions()
        {
            foreach (var packageName in TelemetryPackageNames)
            {
                var version = await GetLatestPackageVersionAsync(packageName);
                _packageToMaxVersion[packageName] = version;
            }
        }

        private async Task<string> GetLatestPackageVersionAsync(string packageName)
        {
            var cache = new SourceCacheContext();
            var cancellationToken = CancellationToken.None;

            var nugetRepo = Repository.Factory.GetCoreV3(NugetUrl);

            var packageFinder = await nugetRepo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            var versions = await packageFinder.GetAllVersionsAsync(
                packageName, cache, NullLogger.Instance,
                cancellationToken);

            return versions.Max(version => version.Version).ToString();
        }

        public static IEnumerable<string> GetProjectFiles(string directory)
        {
            var projectFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".csproj"));
            return projectFiles.Where(x => !x.Contains("buildtasks") && !x.Contains(@"\obj\"));
        }
    }
}
