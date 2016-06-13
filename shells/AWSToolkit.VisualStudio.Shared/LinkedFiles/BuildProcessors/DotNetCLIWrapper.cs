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
        public void Publish(string outputLocation, string targetFramework, string iisAppPath, string configuration, IBuildAndDeploymentLogger logger)
        {
            logger.OutputMessage(string.Format("...invoking 'dotnet publish', working folder '{1}'", outputLocation), 
                                 true, true);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet publish",
                Arguments = string.Format(dotNetPublishArgsTemplate,
                                          targetFramework,
                                          outputLocation,
                                          configuration,
                                          Path.Combine(_projectLocation, "project.json")),

                WorkingDirectory = this._projectLocation,
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = new Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                if (psi.RedirectStandardOutput)
                {
                    while (true)
                    {
                        try
                        {
                            string output;
                            if ((output = proc.StandardOutput.ReadLine()) != null)
                                logger.OutputMessage("......publish: " + output, true, true);
                            string error;
                            if ((error = proc.StandardError.ReadLine()) != null)
                                logger.OutputMessage("......publish error: " + error, true, true);
                            if (output == null && error == null)
                                break;
                        }
                        catch (Exception e)
                        {
                            logger.OutputMessage("......publish exception: " + e.Message, true, true);
                        }
                    }
                }
                else
                    proc.WaitForExit();
            }
        }

        private DotNetCLIWrapper() { }
    }
}