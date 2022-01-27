using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AwsToolkit.VsSdk.Common;

using EnvDTE;
using EnvDTE80;
using log4net;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Publishing
{
    /// <summary>
    /// Common functionality for the "Publish to AWS" menu item
    /// that appears in different VS menus.
    /// </summary>
    public abstract class BasePublishToAwsCommand<T> : BaseCommand<T> where T : BasePublishToAwsCommand<T>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(BasePublishToAwsCommand<T>));

        protected readonly ToolkitContext ToolkitContext;
        protected readonly IAWSToolkitShellProvider ToolkitShellProvider;

        protected DTE2 Dte { get; private set; }
        protected IVsMonitorSelection MonitorSelection { get; private set; }
        protected IVsSolution Solution { get; private set; }
        protected IPublishToAws PublishToAws { get; private set; }
        protected IPublishSettingsRepository PublishSettingsRepository { get; private set; }

        protected BasePublishToAwsCommand(ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository)
        {
            ToolkitShellProvider = toolkitShell;
            ToolkitContext = toolkitContext;
            PublishSettingsRepository = publishSettingsRepository;
        }

        /// <summary>
        /// Overload for testing purposes
        /// </summary>
        protected BasePublishToAwsCommand(
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository,
            DTE2 dte,
            IVsMonitorSelection monitorSelection,
            IVsSolution solution,
            IPublishToAws publishToAws)
            : this(toolkitContext, toolkitShell, publishSettingsRepository)
        {
            Dte = dte;
            MonitorSelection = monitorSelection;
            Solution = solution;
            PublishToAws = publishToAws;
        }

        public static async Task<T> InitializeAsync(
            T command,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            command.Dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(command.Dte);

            command.MonitorSelection = await package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            Assumes.Present(command.MonitorSelection);

            command.Solution = await package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(command.Solution);

            command.PublishToAws = await package.GetServiceAsync(typeof(SPublishToAws)) as IPublishToAws;
            Assumes.Present(command.PublishToAws);

            await InitializeAsync(
                () => command,
                menuGroup, commandId,
                package);

            return command;
        }

        /// <summary>
        /// Starts the Publish to AWS experience
        /// </summary>
        protected override async Task ExecuteAsync(object sender, OleMenuCmdEventArgs args)
        {
            bool success = false;
            string accountId = ToolkitContext.ConnectionManager.ActiveAccountId;
            string regionId = ToolkitContext.ConnectionManager.ActiveRegion?.Id;

            try
            {
                // Working with EnvDTE.Project should be done on UI Thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Until users can manage their connection in the Publish experience,
                // prevent entry if credentials are not already set up, in order
                // to reduce confusion caused by entering into a blank form.
                if (!ToolkitContext.ConnectionManager.IsValidConnectionSettings())
                {
                    ToolkitContext.ToolkitHost.ShowError(
                        "Unable to start Publish to AWS",
                        "AWS Credentials are required to publish your project to AWS.\n\nSelect a valid credentials - region pair in the AWS Explorer before starting the Publish to AWS experience.");

                    success = false;
                    return;
                }

                var project = GetSelectedProject();

                if (project == null)
                {
                    return;
                }

                var publishArgs = new ShowPublishToAwsDocumentArgs()
                {
                    ProjectName = project.Name,
                    ProjectPath = project.FileName,
                    CredentialId = ToolkitContext.ConnectionManager.ActiveCredentialIdentifier,
                    Region = ToolkitContext.ConnectionManager.ActiveRegion,
                };

                await PublishToAws.ShowPublishToAwsDocument(publishArgs);
                success = true;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start the Publish workflow", e);

                ToolkitShellProvider.OutputToHostConsole($"Unable to start the Publish to AWS process: {e.Message}",
                    true);
                ToolkitShellProvider.ShowMessage("Unable to Publish to AWS",
                    string.Format("There was a problem trying to start the Publish process:{0}{0}{1}",
                        Environment.NewLine, e.Message));
                success = false;
            }
            finally
            {
                RecordPublishStartMetric(accountId, regionId, success ? Result.Succeeded : Result.Failed);
            }
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs args)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                menuCommand.Visible = false;

                if (Dte.Solution.IsOpen == false)
                {
                    return;
                }

                if (Dte.SelectedItems.MultiSelect || Dte.SelectedItems.Count < 1)
                {
                    return;
                }

                var selectedProject = GetSelectedProject();
                if (!CanExecute(selectedProject))
                {
                    return;
                }

                menuCommand.Text = GetMenuText(selectedProject);
                menuCommand.Visible = true;
            }
            catch (Exception e)
            {
                // Do not log spam - this could get called many times
                Debug.Assert(false, $"Unable to determine Publish to AWS visibility: {e.Message}");
            }
        }

        /// <summary>
        /// Determines the current Project of interest (else null) for this command
        /// </summary>
        protected Project GetSelectedProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var hierarchy = MonitorSelection.GetCurrentSelectionVsHierarchy(out uint itemId);
            if (!VsHierarchyHelpers.TryResolvingToProject(hierarchy, itemId, out Project project))
            {
                return null;
            }

            return project;
        }


        /// <summary>
        /// Determines what the Menu item's text should be
        /// </summary>
        protected abstract string GetMenuText(Project project);

        /// <summary>
        /// Indicates whether or not the provided project is a candidate for the
        /// Publish to AWS workflow.
        /// </summary>
        protected bool CanExecute(Project project)
        {
            if (project == null)
            {
                return false;
            }

            if (IsLambdaProject(project))
            {
                return false;
            }

            // Deploy CLI does not support .NET Framework projects
            if (!IsProjectPublishable(project))
            {
                return false;
            }

            // TODO : Add any additional criteria for projects to not show the menu item

            return true;
        }

        private bool IsLambdaProject(Project project)
        {
            try
            {
                if (VSUtility.IsLambdaNetProject(project))
                {
                    return true;
                }

                if (VSUtility.IsLambdaNodeJsProject(MonitorSelection))
                {
                    return true;
                }
            }
            catch
            {
                // Swallow error for stability 
            }
            return false;
        }

        private bool IsProjectPublishable(Project project)
        {
            if (Solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy) != VSConstants.S_OK)
            {
                return false;
            }

            if (!VsHierarchyHelpers.TryGetTargetFramework(hierarchy, VSConstants.VSITEMID_ROOT,
                out var targetFramework))
            {
                return false;
            }

            return PublishableProjectSpecification.IsSatisfiedBy(targetFramework);
        }

        private void RecordPublishStartMetric(string accountId, string regionId, Result result)
        {
            ToolkitContext.TelemetryLogger.RecordPublishStart(new PublishStart()
            {
                AwsAccount = accountId,
                AwsRegion = regionId,
                Result = result,
            });
        }
    }
}
