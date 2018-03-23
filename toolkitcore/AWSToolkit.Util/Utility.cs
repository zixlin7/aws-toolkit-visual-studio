using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

using log4net;
using log4net.Config;
using ThirdParty.Json.LitJson;
using System.Text;
using System.Diagnostics;

namespace Amazon.AWSToolkit
{
    public static class Utility
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(Utility));

        public static Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var pos = args.Name.IndexOf(",");
            if (pos > 0)
            {
                var assemblyName = args.Name.Substring(0, pos);
                var extensionRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string testPath = Path.Combine(extensionRoot, assemblyName + ".dll");

                if (File.Exists(testPath))
                    return Assembly.LoadFile(testPath);
            }
            
            return null;
        }

        public static void ConfigureLog4Net()
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fullPath = string.Format(@"{0}\log4net.config", directory);
            if (File.Exists(fullPath))
            {
                XmlConfigurator.ConfigureAndWatch(new FileInfo(fullPath));
            }
        }

        /// <summary>
        /// Inspect the user's machine to verify that a supported version of msdeploy.exe is installed (v1 thru v3)
        /// </summary>
        /// <returns>True if msdeploy found, false otherwise</returns>
        public static bool ProbeForMSDeploy()
        {
            bool installed = false;
            // first try a registry probe on the install settings for IIS Extensions
            RegistryKey msDeployIISKey = null;
            RegistryKey versionSubKey = null;

            try
            {
                msDeployIISKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\IIS Extensions\MSDeploy", false);
                if (msDeployIISKey != null)
                {
                    string[] versions = new string[] { "3", "2", "1" };
                    for (int i = 0; i < versions.Length && !installed; i++)
                    {
                        LOGGER.InfoFormat("Probing for MSDeploy v{0} subkey in IIS Extensions key", versions[i]);
                        versionSubKey = msDeployIISKey.OpenSubKey(versions[i], false);
                        if (versionSubKey != null)
                        {
                            string installFolder = QueryMsDeployInstallFolder(versionSubKey);
                            if (!string.IsNullOrEmpty(installFolder))
                            {
                                LOGGER.InfoFormat("Found key, retrieved install path as {0}", installFolder);
                                installed = true;
                            }
                        }
                        else
                            LOGGER.InfoFormat("Failed to access msdeploy v{0} subkey in IIS Extensions key", versions[i]);
                    }
                }
                else
                    LOGGER.Info("Failed to open IIS Extensions key when probing for msdeploy.exe.");
            }
            catch (Exception e)
            {
                LOGGER.InfoFormat("Caught exception in registry probe for msdeploy, message = {0}", e.Message);
            }
            finally
            {
                if (versionSubKey != null)
                    versionSubKey.Close();
                if (msDeployIISKey != null)
                    msDeployIISKey.Close();
            }

            if (!installed)
                LOGGER.Info("Declaring msdeploy.exe to not be installed.");

            return installed;
        }

        static string QueryMsDeployInstallFolder(RegistryKey msdeployKey)
        {
            string installFolder = msdeployKey.GetValue("InstallPath") as string;
            // suppose we could check for file existence too if we wanted to be really paranoid...
            if (string.IsNullOrEmpty(installFolder))
            {
                installFolder = msdeployKey.GetValue("InstallPath_x86") as string;
            }

            return installFolder;
        }

        /// <summary>
        /// A collection of known paths for common utilities that are usually not found in the path
        /// </summary>
        static readonly IDictionary<string, string> KNOWN_LOCATIONS = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"msdeploy.exe", @"C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe" },
            {"iisreset.exe", @"C:\Windows\System32\iisreset.exe" },
            {"sc.exe", @"C:\Windows\System32\sc.exe" },
            {"dotnet.exe", @"C:\Program Files\dotnet\dotnet.exe" },
            {"taskkill.exe", @"C:\Windows\System32\taskkill.exe" }
        };

        /// <summary>
        /// Search the path environment variable for the command given.
        /// </summary>
        /// <param name="command">The command to search for in the path</param>
        /// <returns>The full path to the command if found otherwise it will return null</returns>
        public static string FindExecutableInPath(string command)
        {
            Func<string, string> quoteRemover = x =>
            {
                if (x.StartsWith("\""))
                    x = x.Substring(1);
                if (x.EndsWith("\""))
                    x = x.Substring(0, x.Length - 1);
                return x;
            };

            if (File.Exists(command))
                return Path.GetFullPath(command);

            var envPath = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in envPath.Split(Path.PathSeparator))
            {
                try
                {
                    var fullPath = Path.Combine(quoteRemover(path), command);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                catch(Exception e)
                {
                    LOGGER.Error("Error combining path with \"" + path + "\" and \"" + command + "\"", e);
                }
            }

            if (KNOWN_LOCATIONS.ContainsKey(command) && File.Exists(KNOWN_LOCATIONS[command]))
                return KNOWN_LOCATIONS[command];

            return null;
        }

        public static void AddDotnetCliToolReference(string projectFilePath, string nuGetPackageName)
        {
            // Skip VS 2015 .NET Core project format and Node.js projects
            var projectExt = Path.GetExtension(projectFilePath);
            if (string.Equals(projectExt, ".xproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(projectExt, ".njsproj", StringComparison.OrdinalIgnoreCase))
                return;

            var versionContent = S3FileFetcher.Instance.GetFileContent("nuget-versions.json");
            if (string.IsNullOrEmpty(versionContent))
                return;

            var data = JsonMapper.ToObject(versionContent);
            if (data[nuGetPackageName] == null)
                return;

            var version = data[nuGetPackageName].ToString();

            var content = File.ReadAllText(projectFilePath);
            if (!content.Contains(nuGetPackageName) && content.StartsWith("<Project Sdk="))
            {
                content = content.Replace("</Project>",
@"
  <ItemGroup>
    <DotNetCliToolReference Include=""NUGET_PACKAGE"" Version=""NUGET_VERSION"" />
  </ItemGroup>
</Project>
");
                content = content.Replace("NUGET_PACKAGE", nuGetPackageName);
                content = content.Replace("NUGET_VERSION", version);

                File.WriteAllText(projectFilePath, content);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                bool changed = false;
                string line = null;
                using (var reader = new StringReader(content))
                {
                    while((line = reader.ReadLine()) != null)
                    {
                        var changedLine = line;
                        if(line.Contains("DotNetCliToolReference") && line.Contains(nuGetPackageName))
                        {
                            var startPos = line.IndexOf("Version=\"");
                            if(startPos != -1)
                            {
                                startPos += "Version=\"".Length;
                            }

                            var endPos = line.IndexOf("\"", startPos + 1);
                            var currentVersion = line.Substring(startPos, endPos - startPos);
                            if(!string.Equals(currentVersion, version, StringComparison.Ordinal))
                            {
                                changedLine = changedLine.Replace(currentVersion, version);
                                changed = true;
                            }
                        }

                        sb.AppendLine(changedLine);
                    }

                    content = sb.ToString();
                }
                

                if(changed)
                {
                    File.WriteAllText(projectFilePath, content);
                }
            }
        }

        public static void LaunchXRayHelp(bool isDotNet)
        {
            var url = isDotNet ? "https://github.com/aws/aws-xray-sdk-dotnet" : "https://docs.aws.amazon.com/xray/latest/devguide/aws-xray.html";
            Process.Start(new ProcessStartInfo(url));
        }

        public static string PrettyPrintJson(string json)
        {
            var data = JsonMapper.ToObject(json);
            return PrettyPrintJson(data);
        }

        public static string PrettyPrintJson(JsonData data)
        {
            using (var writer = new StringWriter())
            {
                var jsonWriter = new JsonWriter(writer) { PrettyPrint = true };
                data.ToJson(jsonWriter);
                return writer.ToString().Trim();
            }
        }
    }
}
