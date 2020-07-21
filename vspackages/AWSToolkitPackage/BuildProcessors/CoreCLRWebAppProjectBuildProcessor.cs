using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.Shared;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.VisualStudio.BuildProcessors
{
    /// <summary>
    /// Class that knows how to build a CoreCLR web application project
    /// prior to deployment.
    /// </summary>
    internal class CoreCLRWebAppProjectBuildProcessor : BuildProcessorBase, IBuildProcessor, IVsUpdateSolutionEvents
    {
        const string ANALYTICS_VALUE = "NET_CORE";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CoreCLRWebAppProjectBuildProcessor));

        string _outputPackage = string.Empty;

        #region IBuildProcessor

        /// <summary>
        /// See https://dotnet.github.io/docs/core-concepts/core-sdk/cli/dotnet-publish.html for details
        /// on the command line we ultimately invoke.
        /// </summary>
        /// <param name="taskInfo"></param>
        void IBuildProcessor.Build(BuildTaskInfo taskInfo)
        {
            TaskInfo = taskInfo;

            try
            {
                // the solution and project build configuration "names" encode the configuration name and the platform, separated 
                // by '|'
                var solutionConfigurationName = taskInfo.Options[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] as string;

                // We need to map the solution config name to the 'equivalent' project build configuration, 
                // as they do not need to be the same. MSBuild prefers the project name, the automation 
                // api wants the solution config name.
                var allConfigurations = taskInfo.Options[DeploymentWizardProperties.SeedData.propkey_ProjectBuildConfigurations] as IDictionary<string, string>;

                TaskInfo.Logger.OutputMessage(string.Format("..publishing configuration '{0}' for project '{1}'", solutionConfigurationName, taskInfo.ProjectInfo.ProjectName), true);
                StartStatusBarBuildFeedback((short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build, string.Format("Publishing configuration '{0}'...", solutionConfigurationName));

                var projectConfigurationAndPlatform = allConfigurations[solutionConfigurationName].Split('|');
                var platform = projectConfigurationAndPlatform[1];
                if (platform.Equals("Any CPU", StringComparison.Ordinal))
                    platform = "AnyCPU";

                var outputLocation = ConstructPackageTempDir(TaskInfo.ProjectInfo);
                if (Directory.Exists(outputLocation))
                    new DirectoryInfo(outputLocation).Delete(true);

                var packagingLocation = Path.Combine(outputLocation, "package");
                string iisAppPath = null;
                if (taskInfo.Options.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
                    iisAppPath = taskInfo.Options[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;

                var dotnetCLIWrapper = new DotNetCLIWrapper(TaskInfo.ProjectInfo.VsProjectLocation);

                var success = dotnetCLIWrapper.Publish(packagingLocation,
                                         TaskInfo.TargetFramework,
                                         projectConfigurationAndPlatform[0],
                                         TaskInfo.Logger) == 0;

                if (success)
                {
                    TaskInfo.Logger.OutputMessage("..preparing up aws deployment manifest file");
                    SetupAWSDeploymentManifest(packagingLocation, iisAppPath);

                    TaskInfo.Logger.OutputMessage("..zipping publishing directory");
                    this._outputPackage = Path.Combine(outputLocation,
                        taskInfo.ProjectInfo.ProjectName + "-" + DateTime.Now.Ticks + ".zip");
                    ZipUtil.CreateZip(_outputPackage, packagingLocation);

                    if (File.Exists(_outputPackage))
                    {
                        ToolkitEvent sizeEvent = new ToolkitEvent();
                        sizeEvent.AddProperty(MetricKeys.DeploymentBundleSize,
                            new FileInfo(this._outputPackage).Length);
                        SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(sizeEvent);

                        ProcessorResult = ResultCodes.Succeeded;

                        TaskInfo.Logger.OutputMessage("..deployment package created successfully...");
                    }
                    else
                    {
                        taskInfo.Logger.OutputMessage(
                            string.Format("...error, package '{0}' could not be found", _outputPackage), true, true);

                        TaskInfo.Logger.OutputMessage("..build fail, unable to find expected deployment package.");
                    }
                }

                if (success && ProcessorResult == ResultCodes.Succeeded)
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.WebApplicationBuildSuccess, ANALYTICS_VALUE);
                    evnt.AddProperty(AttributeKeys.DeploymentNetCoreTargetFramework, TaskInfo.TargetFramework);
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }
                else
                {
                    var msg = "Error executing the dotnet publish command, stopping deployment";
                    LOGGER.ErrorFormat(msg);
                    taskInfo.Logger.OutputMessage(msg);

                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.WebApplicationBuildError, ANALYTICS_VALUE);
                    evnt.AddProperty(AttributeKeys.DeploymentNetCoreTargetFramework, TaskInfo.TargetFramework);
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }
            }
            catch (Exception exc)
            {
                var msg = string.Format("...caught exception during deployment package creation - {0}", exc.Message);
                LOGGER.ErrorFormat(msg);
                TaskInfo.Logger.OutputMessage(msg);

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.WebApplicationBuildError, ANALYTICS_VALUE);
                evnt.AddProperty(AttributeKeys.DeploymentNetCoreTargetFramework, TaskInfo.TargetFramework);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
            }
            finally
            {
                EndStatusBarBuildFeedback();

                TaskInfo.CompletionSignalEvent.Set();
                TaskInfo = null;
            }
        }

        ResultCodes IBuildProcessor.Result => ProcessorResult;

        string IBuildProcessor.DeploymentPackage => _outputPackage;

        #endregion

        #region IVsUpdateSolutionEvents Members

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            LOGGER.InfoFormat("IVsUpdateSolutionEvents.UpdateSolution_Done, fSucceeded={0}, fModified={1}, fCancelCommand={2}", fSucceeded, fModified, fCancelCommand);

            BuildStageSucceeded = fSucceeded != 0;
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        #endregion

        /// <summary>
        /// Set a custom package root folder for the archive, collapsing the default
        /// (and sometimes excessive) paths within the archive structure to the single
        /// root folder.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <returns></returns>
        internal static string ConstructPackageTempDir(VSWebProjectInfo projectInfo)
        {
            return Path.Combine(Path.GetTempPath(),
                                string.Format("AWSDeploy.{0}",
                                              ExtractPrimaryGuidComponent(projectInfo.ProjectIDGuid)));
        }

        /// <summary>
        /// Returns the first, or primary, component of the guid that identifies
        /// the project
        /// </summary>
        /// <param name="projectGuid"></param>
        /// <returns></returns>
        internal static string ExtractPrimaryGuidComponent(string projectGuid)
        {
            var startIndex = 0;
            if (projectGuid.StartsWith("{"))
                startIndex++;

            var firstHyphen = projectGuid.IndexOf('-');
            return projectGuid.Substring(startIndex, firstHyphen - 1);
        }


        const string DEFAULT_MANIFEST = @"
{
  ""manifestVersion"": 1,
  ""deployments"": {

    ""aspNetCoreWeb"": [
      {
        ""name"": ""app"",
        ""parameters"": {
          ""appBundle"": ""."",
          ""iisPath"": ""{iisPath}"",
          ""iisWebSite"": ""{iisWebSite}""
        }
      }
    ]
  }
}
";

        private static void SetupAWSDeploymentManifest(string publishLocation, string iisPath)
        {
            string iisWebSite, iisAppPath;
            int pos = iisPath.IndexOf("/");
            if (pos == -1)
            {
                iisWebSite = "Default Web Site";
                iisAppPath = "/" + iisPath;
            }
            else
            {
                iisWebSite = iisPath.Substring(0, pos);
                iisAppPath = iisPath.Substring(pos);
            }

            var pathToManifest = Path.Combine(publishLocation, "aws-windows-deployment-manifest.json");
            string manifest;
            if (File.Exists(pathToManifest))
            {
                Func<string, JsonData, JsonData> getOrCreateNode = (name, node) =>
                {
                    JsonData child = node[name] as JsonData;
                    if(child == null)
                    {
                        child = new JsonData();
                        node[name] = child;
                    }
                    return child;
                };

                JsonData root = JsonMapper.ToObject(File.ReadAllText(pathToManifest));
                if (root["manifestVersion"] == null || !root["manifestVersion"].IsInt)
                {
                    root["manifestVersion"] = 1;
                }

                JsonData deploymentNode = getOrCreateNode("deployments", root);

                JsonData aspNetCoreWebNode = getOrCreateNode("aspNetCoreWeb", deploymentNode);

                JsonData appNode;
                if (aspNetCoreWebNode.GetJsonType() == JsonType.None || aspNetCoreWebNode.Count == 0)
                {
                    appNode = new JsonData();
                    aspNetCoreWebNode.Add(appNode);
                }
                else 
                    appNode = aspNetCoreWebNode[0];


                if (appNode["name"] == null || !appNode["name"].IsString || string.IsNullOrEmpty((string)appNode["name"]))
                {
                    appNode["name"] = "app";
                }

                JsonData parametersNode = getOrCreateNode("parameters", appNode);
                parametersNode["appBundle"] = ".";
                parametersNode["iisPath"] = iisAppPath;
                parametersNode["iisWebSite"] = iisWebSite;

                manifest = root.ToJson();
            }
            else
            {
                manifest = DEFAULT_MANIFEST.Replace("{iisPath}", iisAppPath).Replace("{iisWebSite}", iisWebSite);

                if (File.Exists(pathToManifest))
                    File.Delete(pathToManifest);
            }

            File.WriteAllText(pathToManifest, manifest);
        }
    }
}
