using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

using log4net;
using log4net.Config;

namespace Amazon.AWSToolkit.VisualStudio.Shared
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
                string testPath;

                // all sdk assemblies should be in .\sdk subfolder
                if (assemblyName.StartsWith("AWSSDK.", StringComparison.OrdinalIgnoreCase))
                {
                    testPath = Path.Combine(extensionRoot, "SDK", assemblyName + ".dll");
                    if (File.Exists(testPath))
                    {
                        LOGGER.InfoFormat("...resolved for {0}", testPath);
                        return Assembly.LoadFile(testPath);
                    }
                }

                testPath = Path.Combine(extensionRoot, assemblyName + ".dll");
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

            if (File.Exists(command))
                return Path.GetFullPath(command);

            var envPath = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in envPath.Split(';'))
            {
                var fullPath = Path.Combine(path, command);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            if (KNOWN_LOCATIONS.ContainsKey(command) && File.Exists(KNOWN_LOCATIONS[command]))
                return KNOWN_LOCATIONS[command];

            return null;
        }
    }
}
