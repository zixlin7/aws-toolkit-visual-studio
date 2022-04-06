using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Reflection;

using Amazon.Runtime.Internal.Settings;

using log4net;

namespace Amazon.AWSToolkit.EC2.ConnectionUtils
{
    internal class ToolsUtil
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ToolsUtil));
        const int LENGTH_TILL_DELETE_CONFIG = 10 * 1000;
        const string CONVERTING_APP = "PemToPPKConverter.exe";

        internal static void SetupThreadToDeleteFile(string file)
        {
            ThreadPool.QueueUserWorkItem((WaitCallback)(x =>
            {
                Thread.Sleep(LENGTH_TILL_DELETE_CONFIG);
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Error deleting file " + file, e);
                }
            }));
        }

        public static string FindTool(string executable)
        {
            return FindTool(executable, string.Empty);
        }

        public static string FindTool(string executable, string additionalSeachFolders)
        {
            var userPreferences = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.UserPreferences);
            var locations = userPreferences["Locations"];

            string path = locations[executable];
            if (path == null || !File.Exists(path))
            {
                path = searchPath(executable, additionalSeachFolders);
                if (path == null)
                {
                    path = searchPath(executable);
                }
            }

            return path;
        }

        static string searchPath(string executable)
        {
            string found = searchPath(executable, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
            if (found != null)
                return found;
            found = searchPath(executable, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));

            return found;
        }

        static string searchPath(string executable, string pathValue)
        {
            executable = @"\" + executable.ToLower();
            if (pathValue == null)
                return null;

            var folders = pathValue.Split(';');
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                    continue;

                foreach (var filename in Directory.GetFiles(folder))
                {
                    if (filename.ToLower().EndsWith(executable))
                        return filename;
                }
            }

            return null;
        }

        public static void SetToolLocation(string executable, string location)
        {
            var userPreferences = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.UserPreferences);
            var locations = userPreferences["Locations"];
            locations[executable] = location;
            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.UserPreferences, userPreferences);
        }

        internal static string WritePEMToPPKFile(string rsaPrivateKey)
        {
            // Temp file paths created by Path.GetTempFileName() created frequent PemToPPKConverter.exe
            // failures, though no error information beyond the exit code was emitted.  Path.GetTempFileName()
            // actually creates the files during the call which, speculatively, may have had a lingering lock
            // or some other behavior that prevented PemToPPKConverter.exe from writing to ppkFile.
            string pemFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string ppkFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                using (StreamWriter writer = new StreamWriter(pemFile))
                {
                    writer.Write(rsaPrivateKey);
                }

                string pluginsFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                string executeablePath = Path.Combine(pluginsFolder, CONVERTING_APP);
                string arguments = string.Format("\"{0}\" \"{1}\"", pemFile, ppkFile);

                using (Process convertProc = new Process())
                {
                    convertProc.StartInfo.FileName = executeablePath;
                    convertProc.StartInfo.Arguments = arguments;
                    convertProc.Start();

                    convertProc.WaitForExit(5 * 1000);

                    if (!convertProc.HasExited || convertProc.ExitCode != 200)
                    {
                        throw new ApplicationException("Unknown error converting pem file to ppk file.  Exit Code: " + convertProc.ExitCode);
                    }
                }
            }
            finally
            {
                File.Delete(pemFile);
            }

            return ppkFile;
        }
    }
}
