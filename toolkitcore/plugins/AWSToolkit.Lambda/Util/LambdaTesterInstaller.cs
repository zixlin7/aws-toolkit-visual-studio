using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Amazon.Common.DotNetCli.Tools;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.AWSToolkit.Lambda.Util
{
    public static class LambdaTesterInstaller
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(LambdaTesterInstaller));
        const string LAUNCH_SETTINGS_FILE = "launchSettings.json";

        const string LAMBDA_TESTER_PACKAGE = "Amazon.Lambda.Tester-2.1";
        const string LAMBDA_TESTER_EXE = "dotnet-lambda-tester-2.1.exe";

        static readonly HashSet<string> SUPPORTED_TARGET_FRAMEWORKS = new HashSet<string> { "netcoreapp2.1" };

        public static void Install(string projectPath)
        {
            try
            {
                if (!File.ReadAllText(projectPath).Contains("<AWSProjectType>Lambda</AWSProjectType>"))
                    return;

                var lambdaTesterInstallpath = GetLambdaTesterPath(Path.GetDirectoryName(projectPath));
                if (string.IsNullOrEmpty(lambdaTesterInstallpath) || !File.Exists(lambdaTesterInstallpath))
                    return;

                var targetFramework = GetTargetFramework(projectPath);
                if (string.IsNullOrEmpty(targetFramework) || !SUPPORTED_TARGET_FRAMEWORKS.Contains(targetFramework))
                    return;

                var getCurrentLaunchConfig = GetLaunchSettings(projectPath);

                var root = JsonConvert.DeserializeObject(getCurrentLaunchConfig) as JObject;

                var profiles = root["profiles"] as JObject;
                if (profiles == null)
                {
                    profiles = new JObject();
                    root["profiles"] = profiles;
                }

                var lambdaTester = profiles["LambdaTester"];
                if (lambdaTester == null)
                {
                    lambdaTester = new JObject();
                    lambdaTester["commandName"] = "Executable";
                    lambdaTester["commandLineArgs"] = "--port 5050";

                    profiles["LambdaTester"] = lambdaTester;
                }

                lambdaTester["executablePath"] = $"{lambdaTesterInstallpath}";
                lambdaTester["workingDirectory"] = $".\\bin\\Debug\\{targetFramework}";

                var updated = JsonConvert.SerializeObject(root, Formatting.Indented);
                SaveLaunchSettings(projectPath, updated);
            }
            catch (Exception e)
            {
                LOGGER.Error($"Error configuring Lambda tester for {projectPath}", e);
            }
        }

        private static string GetLambdaTesterPath(string projectDirectory)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var fullPath = Path.Combine(userProfile, ".dotnet", "tools", LAMBDA_TESTER_EXE);

            if (!File.Exists(fullPath))
            {
                if (RunToolCommand(projectDirectory, "install") != 0)
                {
                    return null;
                }
            }
            else
            {
                RunToolCommand(projectDirectory, "update");
            }

            return fullPath;
        }

        private static string GetTargetFramework(string projectPath)
        {
            var xdoc = XDocument.Parse(File.ReadAllText(projectPath));
            var targetFramework = xdoc.XPathSelectElement("//PropertyGroup/TargetFramework")?.Value;
            return targetFramework;
        }



        private static string GetLaunchSettings(string projectPath)
        {
            var parent = Path.GetDirectoryName(projectPath);
            var properties = Path.Combine(parent, "Properties");
            if (!Directory.Exists(properties))
            {
                Directory.CreateDirectory(properties);
                return "{}";
            }

            var fullPath = Path.Combine(properties, LAUNCH_SETTINGS_FILE);
            if (!File.Exists(fullPath))
                return "{}";

            return File.ReadAllText(fullPath);
        }

        private static void SaveLaunchSettings(string projectPath, string content)
        {
            var fullPath = Path.Combine(Path.GetDirectoryName(projectPath), "Properties", LAUNCH_SETTINGS_FILE);
            File.WriteAllText(fullPath, content);
        }

        private static int RunToolCommand(string projectDirectory, string command)
        {
            var dotnetCLI = DotNetCLIWrapper.FindExecutableInPath("dotnet.exe");
            if (dotnetCLI == null)
                return -10;


            var arguments = $"tool {command} -g {LAMBDA_TESTER_PACKAGE}";
            var psi = new ProcessStartInfo
            {
                FileName = dotnetCLI,
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectDirectory
            };

            StringWriter writer = new StringWriter();
            var handler = (DataReceivedEventHandler)((o, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                writer.WriteLine(e.Data);
            });
            int exitCode;
            using (var proc = new Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                if (psi.RedirectStandardOutput)
                {
                    proc.ErrorDataReceived += handler;
                    proc.OutputDataReceived += handler;
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    proc.EnableRaisingEvents = true;
                }

                proc.WaitForExit();

                exitCode = proc.ExitCode;
            }

            LOGGER.Info($"dotnet {arguments.ToString()}");
            LOGGER.Info(writer.ToString());

            return exitCode;
        }

    }
}