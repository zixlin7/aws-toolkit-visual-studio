using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Build.Framework;
using MSBuildProject = Microsoft.Build.Evaluation.Project;
using EnvDTEProject = EnvDTE.Project;

using Amazon.AWSToolkit.MobileAnalytics;

using ServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Amazon.AwsToolkit.VsSdk.Common;

namespace Amazon.AWSToolkit.VisualStudio.BuildProcessors
{
    /// <summary>
    /// Class that knows how to build a web application project
    /// prior to deployment. This type of IDE projct has an actual 
    /// MSBuild-compatible project file we can request be built.
    /// </summary>
    internal class WebAppProjectBuildProcessor : BuildProcessorBase, IBuildProcessor, IVsUpdateSolutionEvents
    {
        const string ANALYTICS_VALUE = "WEB_APP";

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(WebAppProjectBuildProcessor));

        string _outputPackage = string.Empty;

        #region IBuildProcessor

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

                // If this is a retry due to initial build stage failure, retry the project build otherwise resume from 
                // the packaging stage.
                if (taskInfo.IsFirstAttempt || !BuildStageSucceeded.GetValueOrDefault())
                {
                    TaskInfo.Logger.OutputMessage(string.Format("..building configuration '{0}' for project '{1}'", solutionConfigurationName, taskInfo.ProjectInfo.ProjectName), true);

                    object value;
                    taskInfo.ProjectInfo.VsHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out value);
                    var dteProject = value as EnvDTE.Project;

                    var solnBuildManager = taskInfo.HostServiceProvider(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
                    // because we do a two-stage build, we don't want to run the package stage if the initial build fails -
                    // the simplest way to detect this is via solution events
                    uint solutionEventsCookie;
                    solnBuildManager.AdviseUpdateSolutionEvents(this, out solutionEventsCookie);

                    var dte = taskInfo.HostServiceProvider(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                    var solution = dte.Solution;
                    var solutionBuild2 = (EnvDTE80.SolutionBuild2)solution.SolutionBuild;

                    solutionBuild2.BuildProject(solutionConfigurationName, dteProject.FullName, false);

                    WaitOnBuildCompletion(solnBuildManager);

                    solnBuildManager.UnadviseUpdateSolutionEvents(solutionEventsCookie);

                    if (!BuildStageSucceeded.GetValueOrDefault())
                        LOGGER.ErrorFormat("Project build failed to complete.");
                }

                if (BuildStageSucceeded.GetValueOrDefault())
                {
                    if (taskInfo.IsFirstAttempt)
                    {
                        StartStatusBarBuildFeedback((short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build, "Assembling deployment artifacts...");
                        LOGGER.InfoFormat("Project build completed successfully, starting package build");
                    }
                    else
                    {
                        var msg = string.Format("Assembling deployment artifacts (retry #{0})...", taskInfo.BuildAttempt);
                        StartStatusBarBuildFeedback((short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build, msg);
                        LOGGER.InfoFormat("Retry #{0} for package build", taskInfo.BuildAttempt);
                    }

                    var evalProject = taskInfo.ProjectInfo.BuildProject;

                    var projectConfigurationAndPlatform = allConfigurations[solutionConfigurationName].Split('|');
                    evalProject.SetGlobalProperty("Configuration", projectConfigurationAndPlatform[0]);

                    // VS solution files, where we obtained the configuration and platform data, uses
                    // a platform label of 'Any CPU'. MSBuild projects use 'AnyCPU'. MSBuild works around the 
                    // difference when propagating project files but will not build a project with the space
                    // variant of the label - it complains of no outputdir being set. The MS Connect issue on 
                    // this was marked as 'will not be fixed' as of 2009! Therefore we must translate the 
                    // name on the fly so that our MSBuild-based packaging build pass can run.
                    var platform = projectConfigurationAndPlatform[1];
                    if (platform.Equals("Any CPU", StringComparison.Ordinal))
                        platform = "AnyCPU";
                    evalProject.SetGlobalProperty("Platform", platform);

                    evalProject.ReevaluateIfNecessary();

                    var packageBuildInstance = evalProject.CreateProjectInstance();

                    // the dte.BuildSelection hack will have grabbed the output window and changed to Build,
                    // so grab it back...
                    var packageArtifact = TaskInfo.UseIncrementalDeployment
                        ? Path.Combine(packageBuildInstance.GetPropertyValue("PackageArchiveRootDir"), "Archive")
                        : packageBuildInstance.GetPropertyValue("PackageFileName");

                    TaskInfo.Logger.OutputMessage(string.Format("..creating deployment package {0}...", packageArtifact), true);

                    // we require a single-file package for upload to the selected deployment service or a folder 
                    // to commit to Git in incremental mode
                    packageBuildInstance.SetProperty("PackageAsSingleFile", (!TaskInfo.UseIncrementalDeployment).ToString());

                    string packageTempDir = null;
                    if (TaskInfo.UseIncrementalDeployment)
                    {
                        // setting a custom root to avoid path explosion in the archive avoids path length 
                        // normalization problems we see in ngit
                        packageTempDir = ConstructPackageTempDir(TaskInfo.ProjectInfo);
                        packageBuildInstance.SetProperty("_PackageTempDir", packageTempDir);
                    }

                    if (taskInfo.Options.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
                    {
                        var iisAppPath = taskInfo.Options[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;
                        if (iisAppPath != null)
                        {
                            iisAppPath = iisAppPath.Trim();
                            // If there is a trailing slash after the virtual directory remove it. Make
                            // sure not to remove the slash if there is just a Web Site which is why there is
                            // check for more then one slash.
                            if (iisAppPath.EndsWith("/") && iisAppPath.Count(f => f == '/') > 1)
                                iisAppPath = iisAppPath.Substring(0, iisAppPath.Length - 1);
                        }

                        packageBuildInstance.SetProperty("DeployIisAppPath", iisAppPath);
                    }

                    var msbuildLogger = new Loggers.MSBuildLogger(taskInfo.Logger, "....packaging -");
                    var result = packageBuildInstance.Build(new[] {"Build", "Package" }, new ILogger[] {msbuildLogger});

                    if (result)
                    {
                        _outputPackage = Path.Combine(evalProject.DirectoryPath, packageArtifact);
                        LOGGER.InfoFormat("Deployment package build completed, archive expected at '{0}'", _outputPackage);
                    }
                    else
                    {
                        LOGGER.ErrorFormat("Deployment package build failed to complete.");
                        BuildStageSucceeded = false;
                    }

                    // clean up any package temp dir we may have needed; we'll deploy from the project's 
                    // obj folders anyway
                    if (!string.IsNullOrEmpty(packageTempDir) && Directory.Exists(packageTempDir))
                    {
                        try
                        {
                            Directory.Delete(packageTempDir, true);
                        }
                        catch (IOException e)
                        {
                            var msg = string.Format("...unable to delete temp folder {0}, exception {1}", packageTempDir, e.Message);
                            LOGGER.ErrorFormat(msg);
                            TaskInfo.Logger.OutputMessage(msg);
                        }
                    }
                }
                else
                {
                    LOGGER.ErrorFormat("Failed to build project");
                }

                if (BuildStageSucceeded.GetValueOrDefault())
                {
                    if (TaskInfo.UseIncrementalDeployment)
                    {
                        if (Directory.Exists(_outputPackage))
                            ProcessorResult = ResultCodes.Succeeded;
                    }
                    else if (File.Exists(_outputPackage))
                    {
                        ToolkitEvent sizeEvent = new ToolkitEvent();
                        sizeEvent.AddProperty(MetricKeys.DeploymentBundleSize, new FileInfo(this._outputPackage).Length);
                        SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(sizeEvent);

                        ProcessorResult = ResultCodes.Succeeded;
                    }

                    string msg;
                    if (ProcessorResult == ResultCodes.Succeeded)
                        msg = "..deployment package created at " + _outputPackage;
                    else
                        msg = "..build fail, unable to find expected deployment package " + _outputPackage;

                    TaskInfo.Logger.OutputMessage(msg);
                }
            }
            catch (InvalidOperationException exc)
            {
                // check for possible build engine contention and request a retry
                if (exc.Message.StartsWith("The operation cannot be completed because a build is already in progress", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = string.Format("Build engine contention - {0}", exc.Message);
                    LOGGER.ErrorFormat(msg);
                    ProcessorResult = ResultCodes.FailedShouldRetry;
                }
                else
                    throw new Exception("InvalidOperationException during build", exc);
            }
            catch (Exception exc)
            {
                var msg = string.Format("...caught exception during deployment package creation - {0}", exc.Message);
                LOGGER.ErrorFormat(msg);
                TaskInfo.Logger.OutputMessage(msg);
            }
            finally
            {
                ToolkitEvent evnt = new ToolkitEvent();
                if (ProcessorResult == ResultCodes.Succeeded)
                {
                    evnt.AddProperty(AttributeKeys.WebApplicationBuildSuccess, ANALYTICS_VALUE);
                }
                else
                {
                    evnt.AddProperty(AttributeKeys.WebApplicationBuildError, $"{ANALYTICS_VALUE}:{ProcessorResult}");
                }
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

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
            return Path.Combine(Path.GetPathRoot(projectInfo.VsProjectLocation),
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
            return projectGuid.Substring(startIndex, firstHyphen-1);
        }
    }
}
