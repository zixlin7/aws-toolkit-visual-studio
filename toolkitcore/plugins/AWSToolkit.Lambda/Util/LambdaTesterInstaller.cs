using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Amazon.AWSToolkit.Lambda.LambdaTester;
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
            {"netcoreapp2.1", new ToolConfig("Amazon.Lambda.TestTool-2.1", "dotnet-lambda-test-tool-2.1.exe")},
            {"netcoreapp3.1", new ToolConfig("Amazon.Lambda.TestTool-3.1", "dotnet-lambda-test-tool-3.1.exe")},
            {"net5.0", new ToolConfig("Amazon.Lambda.TestTool-5.0", "dotnet-lambda-test-tool-5.0.exe")}
        };

        static readonly ISet<string> InstalledTesterPackages = new HashSet<string>();

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

        /// <summary>
        /// If the Lambda Tester is relevant to this project,
        /// the tester is installed,
        /// then the project's launch configuration is updated.
        /// 
        /// Function is not thread safe; expected to be called sequentially (used by LambdaTesterUtilities).
        /// Unknown if the dotnet installer could handle concurrent executions, and the "tester installed"
        /// state does not have concurrent locking.
        /// </summary>
        public static void Install(string projectPath)
        {
            try
            {
                var project = new Project(projectPath);
                if (!IsLambdaTesterSupported(project))
                {
                    return;
                }

                ToolConfig toolConfig = GetTesterConfiguration(project.TargetFramework);
                if (toolConfig == null)
                {
                    return;
                }

                // Only attempt to install the Lambda Tester once per IDE Session.
                // This prevents slower load times when opening solutions with many projects.
                if (!IsTesterInstalled(toolConfig))
                {
                    var retVal = InstallLambdaTester(toolConfig, Path.GetDirectoryName(projectPath));
                    if (retVal == 0)
                    {
                        MarkTesterInstalled(toolConfig);
                    }
                }
                else
                {
                    LOGGER.Debug($"Lambda Tester already installed: {toolConfig.Package}, Project: {projectPath}");
                }

                var lambdaTestToolInstallPath = GenerateToolConfigCombinedPath("%USERPROFILE%", toolConfig);
                UpdateLaunchSettingsWithLambdaTester(projectPath, lambdaTestToolInstallPath, project.TargetFramework);
            }
            catch (Exception e)
            {
                LOGGER.Error($"Error configuring Lambda tester for {projectPath}", e);
            }
        }

        /// <summary>
        /// Gets the Tester tool associated with a given framework.
        /// </summary>
        /// <returns>Lambda Tester information; null if no applicable tester found</returns>
        public static ToolConfig GetTesterConfiguration(string targetFramework)
        {
            if (string.IsNullOrEmpty(targetFramework))
            {
                return null;
            }

            if (!ToolConfigs.TryGetValue(targetFramework, out var toolConfig))
            {
                return null;
            }

            return toolConfig;
        }

        /// <summary>
        /// Indicates whether or not the Lambda Tester is relevant to this project.
        /// </summary>
        internal static bool IsLambdaTesterSupported(Project project)
        {
            // Short circuit if we know the project doesn't include an AWSProjectType string. 
            if (!project.FileContents.Contains("<AWSProjectType>"))
            {
                return false;
            }

            // The mock test tool does not currently support projects using 
            // Amazon.Lambda.RuntimeSupport for custom runtimes.
            if (project.FileContents.Contains("Amazon.Lambda.RuntimeSupport"))
            {
                return false;
            }

            var awsProjectType = project.AwsProjectType;
            if (string.IsNullOrEmpty(awsProjectType))
            {
                return false;
            }

            if (!awsProjectType.Contains("Lambda"))
            {
                return false;
            }

            if (string.IsNullOrEmpty(project.TargetFramework))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Installs or Updates the dotnet tool
        /// </summary>
        /// <param name="toolConfig">Lambda Tester to install</param>
        /// <param name="workingDir">Where to run the install command from</param>
        /// <returns>Installation return code</returns>
        private static int InstallLambdaTester(ToolConfig toolConfig, string workingDir)
        {
            var installedTesterPath =
                GenerateToolConfigCombinedPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    toolConfig);
            var cmd = File.Exists(installedTesterPath) ? "update" : "install";

            LOGGER.Debug("Attempting to install or update Lambda Tester dotnet tool");
            return RunToolCommand(workingDir, cmd, toolConfig);
        }

        private static void MarkTesterInstalled(ToolConfig toolConfig)
        {
            InstalledTesterPackages.Add(toolConfig.Package);
        }

        private static bool IsTesterInstalled(ToolConfig toolConfig)
        {
            return InstalledTesterPackages.Contains(toolConfig.Package);
        }

        /// <summary>
        /// Initializes the project's launch settings if necessary, and
        /// ensures they are referencing the tester tool's location.
        /// </summary>
        private static void UpdateLaunchSettingsWithLambdaTester(string projectPath, string lambdaTestToolInstallPath,
            string targetFramework)
        {
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
                // TODO : is this a suitable port initialization value? Should they be unique per project?
                lambdaTester["commandLineArgs"] = "--port 5050";

                profiles[LAUNCH_SETTINGS_NODE] = lambdaTester;
            }

            lambdaTester["workingDirectory"] = $".\\bin\\$(Configuration)\\{targetFramework}";
            lambdaTester["executablePath"] = $"{lambdaTestToolInstallPath}";

            var updated = JsonConvert.SerializeObject(root, Formatting.Indented);
            SaveLaunchSettings(projectPath, updated);
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
            var handler = (DataReceivedEventHandler) ((o, e) =>
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

        /// <summary>
        /// Generates a path combining the base path with the toolconfig subpath
        /// </summary>
        /// <returns>combined path</returns>
        private static string GenerateToolConfigCombinedPath(string basePath, ToolConfig toolConfig)
        {
            return Path.Combine(basePath,
                ".dotnet", "tools", toolConfig.ToolExe
            );
        }
    }
}