using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

using log4net;
using log4net.Config;
using log4net.Util.TypeConverters;
using ThirdParty.Json.LitJson;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit
{
    public static class Utility
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Utility));
        public const string LogDirectoryName = "logs";

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

        private static bool _loggingInitialized;

        public static async Task ConfigureLog4NetAsync(ISettingsRepository<LoggingSettings> loggingSettingsRepository)
        {
            if (!_loggingInitialized)
            {
                _loggingInitialized = true;
                //add converter for any properties that target a numeric log4net.config field
                ConverterRegistry.AddConverter(typeof(int), new NumericLog4NetConverter());

                var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var fullPath = $@"{directory}\log4net.config";

                var settings = await loggingSettingsRepository.GetOrDefaultAsync(new LoggingSettings());

                GlobalContext.Properties["MaxLogFileSize"] = settings.MaxLogFileSizeMb.ToString();
                GlobalContext.Properties["MaxFileBackups"] = settings.MaxFileBackups;

                if (File.Exists(fullPath))
                {
                    XmlConfigurator.ConfigureAndWatch(new FileInfo(fullPath));
                }

                //mark and save log settings to the file if it does not exist
                var filePath = GetAppDataSettingsFilePath(typeof(LoggingSettings));
                if (!File.Exists(filePath))
                {
                    loggingSettingsRepository.Save(settings);
                }
            }
        }

        /// <summary>
        /// Executes cleanup of log files based on settings found in "LoggingSettings.json"
        /// Ensures single run of cleanup is performed by using a global mutex when multiple instances are spun up and attempt to run cleanup
        /// </summary>
        public static async Task CleanupLogFilesAsync(ISettingsRepository<LoggingSettings> loggingSettingsRepository)
        {
            var directory = ToolkitAppDataPath.Join(LogDirectoryName);
            if (!_loggingInitialized || !Directory.Exists(directory))
            {
                return;
            }
            
            var settings = await loggingSettingsRepository.GetOrDefaultAsync(new LoggingSettings());
            try
            {
                using (LoggingMutex.Acquire())
                {
                    ExecuteLogCleanup(directory, settings);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to run logging cleanup.", ex);
            }
        }

        /// <summary>
        /// Executes cleanup logic based on the specified settings
        /// for log files found in the the specified directory
        /// NOTE: Cleanup should be invoked only once among multiple concurrent instances
        /// This is achieved by using the global mutex in the calling function
        /// </summary>
        private static void ExecuteLogCleanup(string directory, LoggingSettings settings)
        {
            Logger.Info("Running log cleanup");
            var directoryInfo = new DirectoryInfo(directory);
            var months = settings.LogFileRetentionMonths;
            var sizeLimit = settings.MaxLogDirectorySizeMb * 1024 * 1024;

            //delete files older than the specified number of months
            DeleteOldFiles(directoryInfo, months);

            // maintain max log directory size
            MaintainDirectorySize(directoryInfo, sizeLimit);
        }

        /// <summary>
        /// Deletes files older than the specified number of months from the directory
        /// </summary>
        public static void DeleteOldFiles(DirectoryInfo directoryInfo, int months)
        {
            GetAllFiles(directoryInfo)
                .Where(file => file.LastWriteTime < DateTime.Now.AddMonths(months * -1))
                .ToList()
                .ForEach(f => f.Delete());
        }

        /// <summary>
        /// Maintains max directory size(in bytes) specified by deleting oldest files 
        /// </summary>
        public static void MaintainDirectorySize(DirectoryInfo directoryInfo, long limit)
        {
            var size = GetDirectorySize(directoryInfo);

            //check if directory size is equal to or greater than max log directory size limit(in MB)
            if (size < limit)
            {
                return;
            }

            var orderedFiles = GetAllFiles(directoryInfo).OrderBy(x => x.LastWriteTime);

            // delete oldest files if directory size is greater than max log directory size limit
            foreach (var file in orderedFiles)
            {
                if (size < limit)
                {
                    break;
                }

                //delete file
                file.Delete();
                size = GetDirectorySize(directoryInfo);
            }
        }

        /// <summary>
        /// Gets file path for specified type of settings (Type) in AppData folder
        /// </summary>
        public static string GetAppDataSettingsFilePath(Type type)
        {
            //filename is the name assigned to the settings type
            return ToolkitAppDataPath.Join($"{type.Name}.json");
        }

        private static List<FileInfo> GetAllFiles(DirectoryInfo directoryInfo)
        {
            return directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).ToList();
        }

        private static long GetDirectorySize(DirectoryInfo directoryInfo)
        {
            return GetAllFiles(directoryInfo).Sum(fi => fi.Length);
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
                        Logger.InfoFormat("Probing for MSDeploy v{0} subkey in IIS Extensions key", versions[i]);
                        versionSubKey = msDeployIISKey.OpenSubKey(versions[i], false);
                        if (versionSubKey != null)
                        {
                            string installFolder = QueryMsDeployInstallFolder(versionSubKey);
                            if (!string.IsNullOrEmpty(installFolder))
                            {
                                Logger.InfoFormat("Found key, retrieved install path as {0}", installFolder);
                                installed = true;
                            }
                        }
                        else
                            Logger.InfoFormat("Failed to access msdeploy v{0} subkey in IIS Extensions key", versions[i]);
                    }
                }
                else
                    Logger.Info("Failed to open IIS Extensions key when probing for msdeploy.exe.");
            }
            catch (Exception e)
            {
                Logger.InfoFormat("Caught exception in registry probe for msdeploy, message = {0}", e.Message);
            }
            finally
            {
                if (versionSubKey != null)
                    versionSubKey.Close();
                if (msDeployIISKey != null)
                    msDeployIISKey.Close();
            }

            if (!installed)
                Logger.Info("Declaring msdeploy.exe to not be installed.");

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
                    Logger.Error("Error combining path with \"" + path + "\" and \"" + command + "\"", e);
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

            // Migrate existing projects to the new AWSProjectType property setting
            if(string.Equals("Amazon.Lambda.Tools", nuGetPackageName, StringComparison.OrdinalIgnoreCase))
            {
                var updatedContent = ProjectFileUtilities.SetAWSProjectType(content, "Lambda");
                if(!string.Equals(content, updatedContent, StringComparison.Ordinal))
                {
                    content = updatedContent;
                    File.WriteAllText(projectFilePath, content);
                }
            }

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
