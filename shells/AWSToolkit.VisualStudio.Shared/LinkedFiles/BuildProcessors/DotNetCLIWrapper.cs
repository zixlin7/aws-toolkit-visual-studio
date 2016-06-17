using System;
using System.Text;
using System.Diagnostics;
using System.IO;

using Microsoft.Win32;

using Amazon.AWSToolkit.VisualStudio.Shared.Loggers;

namespace Amazon.AWSToolkit.VisualStudio.Shared.BuildProcessors
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
        public void Publish(string outputLocation, string targetFramework, string configuration, IBuildAndDeploymentLogger logger)
        {
            logger.OutputMessage(string.Format("...invoking 'dotnet publish', working folder '{0}'", outputLocation), 
                                 true, true);

            var psi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\dotnet\dotnet.exe",
                Arguments = string.Format("publish --output \"{0}\" --configuration {1}", outputLocation, configuration),
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
            }
        }

        private DotNetCLIWrapper() { }
    }
}