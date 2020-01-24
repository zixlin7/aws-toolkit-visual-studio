using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BuildTasks
{
    public class UpdateAwsSdkVersionsTask : BuildTaskBase
    {
        const string AWSSDK_VERSION_MANIFEST = "https://raw.githubusercontent.com/aws/aws-sdk-net/master/generator/ServiceModels/_sdk-versions.json";


        public string RootLocation { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var versions = LoadKnownVersions();

            Log.LogMessage($"Searching for projects starting at root {this.RootLocation}");
            var projectFiles = GetProjectFiles(this.RootLocation);
            foreach(var projectFile in projectFiles)
            {
                Log.LogMessage($"Processing {projectFile}");
                var xdoc = new XmlDocument();
                xdoc.Load(projectFile);

                var changed = false;
                var packageReferenceNodes = xdoc.DocumentElement.SelectNodes("//*[local-name()='ItemGroup']/*[local-name()='PackageReference']");
                foreach (XmlElement packageReferenceNode in packageReferenceNodes)
                {
                    var packageId = packageReferenceNode.GetAttribute("Include");
                    if (string.IsNullOrEmpty(packageId) || !versions.ContainsKey(packageId))
                        continue;

                    var latestVersion = versions[packageId];

                    var projectVersion = packageReferenceNode.GetAttribute("Version");
                    if (!string.IsNullOrEmpty(projectVersion))
                    {
                        if (string.Equals(latestVersion, projectVersion))
                            continue;

                        Log.LogMessage($"\tUpdated {packageId}: {projectVersion} -> {latestVersion}");
                        changed = true;
                        packageReferenceNode.SetAttribute("Version", latestVersion);
                    }
                    else if (!string.IsNullOrEmpty((projectVersion = packageReferenceNode["Version"]?.InnerText)))
                    {
                        if (string.Equals(latestVersion, projectVersion))
                            continue;

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

        public IDictionary<string, string> LoadKnownVersions()
        {
            var versions = new Dictionary<string, string>();
            try
            {
                string jsonContent;
                using (var client = new HttpClient())
                {
                    jsonContent = client.GetStringAsync(AWSSDK_VERSION_MANIFEST).Result;
                }

                var root = JsonConvert.DeserializeObject(jsonContent) as JObject;
                versions["AWSSDK.Core"] = root["CoreVersion"].ToString();

                var serviceVersions = root["ServiceVersions"] as JObject;
                foreach (var service in serviceVersions.Properties())
                {

                    var packageId = "AWSSDK." + service.Name;
                    var version = service.Value["Version"]?.ToString();

                    versions[packageId] = version;
                }
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
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
