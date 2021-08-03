using System;
using System.Text;
using System.Diagnostics;

using Amazon.AWSToolkit.VisualStudio.Loggers;

namespace Amazon.AWSToolkit.VisualStudio.BuildProcessors
{
    internal class DotNetCLIWrapper
    {
        #region constants
        const string dotNetPublishArgsTemplate = "--framework {0} --output {1} --configuration {2} {3}";
        #endregion

        string _projectLocation;

        public DotNetCLIWrapper(string projectLocation)
        {
            this._projectLocation = projectLocation;
        }

        /// <summary>
        /// Generates deployment manifest for staged content
        /// </summary>
        /// <param name="outputLocation"></param>
        /// <param name="targetFramework"></param>
        /// <param name="iisAppPath"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public int Publish(string outputLocation, string targetFramework, string configuration, IBuildAndDeploymentLogger logger)
        {
            logger.OutputMessage(string.Format("...invoking 'dotnet publish', working folder '{0}'", outputLocation), 
                                 true, true);

            var dotnetCLI = Utility.FindExecutableInPath("dotnet.exe");
            if (string.IsNullOrEmpty(dotnetCLI))
                throw new Exception("Failed to locate dotnet.exe. Make sure the dotnet CLI is installed in the environment PATH.");

            var psi = new ProcessStartInfo
            {
                FileName = dotnetCLI,
                Arguments = string.Format("publish --output \"{0}\" --configuration {1} -f {2}", outputLocation, configuration, targetFramework),
                WorkingDirectory = this._projectLocation,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = new Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                var outputLog = new StringBuilder();
                var handler = (DataReceivedEventHandler)((o, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;
                    logger.OutputMessage("......publish: " + e.Data, true, true);
                });

                proc.ErrorDataReceived += handler;
                proc.OutputDataReceived += handler;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.EnableRaisingEvents = true;

                proc.WaitForExit();

                return proc.ExitCode;
            }
        }

        private DotNetCLIWrapper() { }
    }
}