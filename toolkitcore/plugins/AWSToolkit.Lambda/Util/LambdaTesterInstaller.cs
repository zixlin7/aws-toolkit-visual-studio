using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        const int DOTNET_NOT_FOUND_ERROR_CODE = -10;

        static ILog LOGGER = LogManager.GetLogger(typeof(LambdaTesterInstaller));
        const string LAUNCH_SETTINGS_FILE = "launchSettings.json";

        const string LAUNCH_SETTINGS_NODE = "Mock Lambda Test Tool";

        static readonly IDictionary<string, ToolConfig> ToolConfigs = new Dictionary<string, ToolConfig>
        {
            { "netcoreapp2.1", new ToolConfig("Amazon.Lambda.TestTool-2.1", "dotnet-lambda-test-tool-2.1.exe" ) },
            { "netcoreapp3.1", new ToolConfig("Amazon.Lambda.TestTool-3.1", "dotnet-lambda-test-tool-3.1.exe" ) }
        };


        public class ToolConfig
        {
            public ToolConfig(string package, string toolExe)
            {
                this.Package = package;
                this.ToolExe = toolExe;
            }

            public string Package { get; private set; }
            public string ToolExe { get; private set; }            
        }

        public static void Install(string projectPath)
        {
            try
            {
                var projectContent = File.ReadAllText(projectPath);
                // Short circuit load the XML document if we know the project doesn't include an 
                // AWSProjectType string. 
                if (!projectContent.Contains("<AWSProjectType>"))
                    return;

                // The mock test tool does not currently support projects using 
                // Amazon.Lambda.RuntimeSupport for custom runtimes.
                if (projectContent.Contains("Amazon.Lambda.RuntimeSupport"))
                    return;

                var xdoc = XDocument.Parse(projectContent);
                var awsProjectType = xdoc.XPathSelectElement("//PropertyGroup/AWSProjectType")?.Value;
                if (string.IsNullOrEmpty(awsProjectType) || !awsProjectType.Contains("Lambda"))
                    return;

                var targetFramework = GetTargetFramework(xdoc);
                ToolConfig toolConfig;
                if (string.IsNullOrEmpty(targetFramework) || !ToolConfigs.TryGetValue(targetFramework, out toolConfig))
                    return;

                var lambdaTestToolInstallpath = GetLambdaTestToolPath(Path.GetDirectoryName(projectPath), toolConfig);
                if (string.IsNullOrEmpty(lambdaTestToolInstallpath))
                    return;


                var getCurrentLaunchConfig = GetLaunchSettings(projectPath);

                var root = JsonConvert.DeserializeObject(getCurrentLaunchConfig) as JObject;

                var profiles = root["profiles"] as JObject;
                if (profiles == null)
                {
                    profiles = new JObject();
                    root["profiles"] = profiles;
                }

                var lambdaTester = profiles[LAUNCH_SETTINGS_NODE];
                if (lambdaTester == null)
                {
                    lambdaTester = new JObject();
                    lambdaTester["commandName"] = "Executable";
                    lambdaTester["commandLineArgs"] = "--port 5050";

                    profiles[LAUNCH_SETTINGS_NODE] = lambdaTester;
                }

                lambdaTester["workingDirectory"] = $".\\bin\\$(Configuration)\\{targetFramework}";
                lambdaTester["executablePath"] = $"{lambdaTestToolInstallpath}";

                var updated = JsonConvert.SerializeObject(root, Formatting.Indented);
                SaveLaunchSettings(projectPath, updated);
            }
            catch (Exception e)
            {
                LOGGER.Error($"Error configuring Lambda tester for {projectPath}", e);
            }
        }

        private static string GetLambdaTestToolPath(string projectDirectory, ToolConfig toolConfig)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var fullPath = Path.Combine(userProfile, ".dotnet", "tools", toolConfig.ToolExe);

            if (!File.Exists(fullPath))
            {
                if (RunToolCommand(projectDirectory, "install", toolConfig) != 0)
                {
                    return null;
                }
            }
            else
            {
                RunToolCommand(projectDirectory, "update", toolConfig);
            }

            var variablePath = Path.Combine(Directory.GetParent(userProfile).FullName, "%USERNAME%", ".dotnet", "tools", toolConfig.ToolExe);
            return variablePath;
        }

        private static string GetTargetFramework(XDocument xdocProject)
        {
            var targetFramework = xdocProject.XPathSelectElement("//PropertyGroup/TargetFramework")?.Value;
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

        private static int RunToolCommand(string projectDirectory, string command, ToolConfig toolConfig)
        {
            var dotnetCLI = DotNetCLIWrapper.FindExecutableInPath("dotnet.exe");
            if (dotnetCLI == null)
                return DOTNET_NOT_FOUND_ERROR_CODE;


            var arguments = $"tool {command} -g {toolConfig.Package}";
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