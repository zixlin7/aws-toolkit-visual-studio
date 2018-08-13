using System;
using System.IO;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.VisualStudio.BuildProcessors
{
    /// <summary>
    /// Class that knows how to build a web site project (ie a project-file-less
    /// project) prior to deployment
    /// </summary>
    internal class WebSiteProjectBuildProcessor : BuildProcessorBase, IBuildProcessor, IVsUpdateSolutionEvents
    {
        const string ANALYTICS_VALUE = "WEB_SITE";

        string _outputPackage = string.Empty;

        const string AspNetTargetPathPropertyPattern = "Project_{0}_AspNetTargetPath";

        #region IBuildProcessor

        void IBuildProcessor.Build(BuildTaskInfo taskInfo)
        {
            try
            {
                this.TaskInfo = taskInfo;

                object value;
                taskInfo.ProjectInfo.VsHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out value);
                var dteProject = value as EnvDTE.Project;

                var solnBuildManager = TaskInfo.HostServiceProvider(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;

                // because we do a two-stage build, we don't want to run the package stage if the initial build fails -
                // the simplest way to detect this is via solution events
                uint solutionEventsCookie;
                solnBuildManager.AdviseUpdateSolutionEvents(this, out solutionEventsCookie);

                // because we have no project file, run a build on the solution instead so any
                // references are taken care of. Note that project settings (in the solution file)
                // mean the build output can be well away from the actual source.
                //dteProject.DTE.ExecuteCommand("Build.BuildSolution", string.Empty);
                var dte = taskInfo.HostServiceProvider(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var solution = dte.Solution;
                var solutionBuild2 = (EnvDTE80.SolutionBuild2)solution.SolutionBuild;

                var configurationName = taskInfo.Options[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] as string;

                solutionBuild2.BuildProject(configurationName, dteProject.FullName, false);

                WaitOnBuildCompletion(solnBuildManager);

                solnBuildManager.UnadviseUpdateSolutionEvents(solutionEventsCookie);

                if (BuildStageSucceeded.GetValueOrDefault())
                {
                    StartStatusBarBuildFeedback((short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build, "Building deployment package...");

                    if (TaskInfo.UseIncrementalDeployment)
                    {
                        _outputPackage = Path.Combine(Path.GetTempPath(), 
                                                        string.Format("AWSDeploy.{0}",
                                                                      WebAppProjectBuildProcessor.ExtractPrimaryGuidComponent(TaskInfo.ProjectInfo.ProjectIDGuid)));
                        // clean the folder if it exists so that Git can spot deleted files
                        if (Directory.Exists(_outputPackage))
                            Directory.Delete(_outputPackage, true);
                    }
                    else
                    {
                        _outputPackage = Path.Combine(Path.GetTempPath(),
                                                        string.Format(MSDeployWrapper.ArchiveNamePattern,
                                                                    TaskInfo.ProjectInfo.ProjectName,
                                                                    TaskInfo.VersionLabel));
                    }

                    // the dte.BuildSelection hack will have grabbed the output window and changed to Build,
                    // so grab it back...
                    taskInfo.Logger.OutputMessage(string.Format("...building deployment package '{0}'", _outputPackage), true, true);

                    var projectTargetPathProperty = string.Format(AspNetTargetPathPropertyPattern,
                                                                  TaskInfo.ProjectInfo.ProjectIDGuid.Trim(new char[] { '{', '}' }));

                    // SteveR: - not ready to make use of this yet
                    string stagedOutputLocation = null /*taskInfo.ProjectInfo.BuildProject.GetPropertyValue(projectTargetPathProperty)*/;
                    if (string.IsNullOrEmpty(stagedOutputLocation))
                        stagedOutputLocation = TaskInfo.ProjectInfo.VsProjectLocation;

                    string iisAppPath = null;
                    if (taskInfo.Options.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
                    {
                        iisAppPath = taskInfo.Options[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;
                    }

                    var msDeployWrapper = new MSDeployWrapper(TaskInfo.ProjectInfo.ProjectName,
                                                              stagedOutputLocation,
                                                              TaskInfo.TargetRuntime,
                                                              iisAppPath,
                                                              TaskInfo.UseIncrementalDeployment);

                    msDeployWrapper.Run(_outputPackage, TaskInfo.Logger);
                }

                if (TaskInfo.UseIncrementalDeployment)
                {
                    if (Directory.Exists(_outputPackage))
                        ProcessorResult = ResultCodes.Succeeded;
                    else
                        taskInfo.Logger.OutputMessage(string.Format("...error, folder '{0}' could not be found", _outputPackage), true, true);
                }
                else
                {
                    if (File.Exists(_outputPackage))
                    {
                        ToolkitEvent sizeEvent = new ToolkitEvent();
                        sizeEvent.AddProperty(MetricKeys.DeploymentBundleSize, new FileInfo(this._outputPackage).Length);
                        SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(sizeEvent);

                        ProcessorResult = ResultCodes.Succeeded;
                    }
                    else
                        taskInfo.Logger.OutputMessage(string.Format("...error, package '{0}' could not be found", _outputPackage), true, true);
                }

                TaskInfo.Logger.OutputMessage(ProcessorResult == ResultCodes.Succeeded
                    ? "..deployment package created successfully..."
                    : "..build fail, unable to find expected deployment package.");
            }
            catch (Exception exc)
            {
                TaskInfo.Logger.OutputMessage("...caught exception during deployment package creation: " + exc.Message, true, true);
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
                    evnt.AddProperty(AttributeKeys.WebApplicationBuildError, ANALYTICS_VALUE);
                }
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                EndStatusBarBuildFeedback();

                TaskInfo.CompletionSignalEvent.Set();
                TaskInfo = null;
            }
        }

        ResultCodes IBuildProcessor.Result
        {
            get { return ProcessorResult; }
        }

        string IBuildProcessor.DeploymentPackage
        {
            get { return _outputPackage; }
        }

        #endregion

        #region IVsUpdateSolutionEvents Members

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            if (fSucceeded != 0)
                BuildStageSucceeded = true;
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion
    }
}
