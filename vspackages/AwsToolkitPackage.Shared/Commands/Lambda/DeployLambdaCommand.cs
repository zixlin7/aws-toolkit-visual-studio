using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Amazon.AwsToolkit.VsSdk.Common;
using Amazon.AWSToolkit.Lambda;
using Amazon.AWSToolkit.Lambda.WizardPages;
using Amazon.AWSToolkit.Shared;

using EnvDTE;

using log4net;

using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Lambda
{
    /// <summary>
    /// Extension command responsible for deploying Lambda projects
    /// and Serverless Application projects.
    /// </summary>
    public class DeployLambdaCommand : BaseCommand<DeployLambdaCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployLambdaCommand));

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly IVsMonitorSelection _monitorSelection;

        /// <summary>
        /// Caches the Lambda plugin. Use <see cref="LambdaPlugin"/> to access.
        /// </summary>
        private IAWSLambda _lambdaPlugin;

        private IAWSLambda LambdaPlugin
        {
            get
            {
                try
                {
                    if (_lambdaPlugin == null)
                    {
                        _lambdaPlugin = _toolkitShell?.QueryAWSToolkitPluginService(typeof(IAWSLambda)) as IAWSLambda;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(
                        "Error looking for AWS Toolkit Lambda Plugin. Lambda functionality may not be available.", e);
                }

                return _lambdaPlugin;
            }
        }

        public DeployLambdaCommand(IAWSToolkitShellProvider toolkitShell, IVsMonitorSelection monitorSelection)
        {
            _toolkitShell = toolkitShell;
            _monitorSelection = monitorSelection;
        }

        public static async Task<DeployLambdaCommand> InitializeAsync(
            IAWSToolkitShellProvider toolkitShell,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            var monitorSelection = await package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            return await InitializeAsync(
                () => new DeployLambdaCommand(toolkitShell, monitorSelection),
                menuGroup, commandId,
                package);
        }

        /// <summary>
        /// Launches the Lambda/Serverless deploy dialog and performs the deployment.
        /// </summary>
        protected override async Task ExecuteAsync(object sender, OleMenuCmdEventArgs args)
        {
            try
            {
                if (LambdaPlugin == null)
                {
                    return;
                }

                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                // selectedProject can be null if the user is right clicking on a serverless.template file.
                var selectedProject = VSUtility.GetSelectedProject();

                var fullPath = selectedProject?.FullName;
                if (fullPath == null)
                {
                    var fileItem = VSUtility.GetSelectedProjectItem();
                    if (string.Equals(fileItem?.Name, Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME))
                    {
                        fullPath = VSUtility.GetSelectedItemFullPath();
                    }
                }

                if (fullPath == null || !File.Exists(fullPath))
                {
                    _toolkitShell.ShowError("The selected item is not a project that can be deployed to AWS Lambda");

                    return;
                }

                var rootDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty;

                var seedProperties = new Dictionary<string, object>();
                seedProperties[UploadFunctionWizardProperties.SelectedProjectFile] = fullPath;
                seedProperties[UploadFunctionWizardProperties.PackageType] = Amazon.Lambda.PackageType.Zip;
                var dockerfilePath = Path.Combine(rootDirectory, "Dockerfile");

                if (selectedProject != null)
                {
                    IDictionary<string, IList<string>> suggestedMethods =
                        VSLambdaUtility.SearchForLambdaFunctionSuggestions(selectedProject);
                    seedProperties[UploadFunctionWizardProperties.SuggestedMethods] = suggestedMethods;
                    seedProperties[UploadFunctionWizardProperties.SourcePath] = rootDirectory;
                    if (File.Exists(dockerfilePath))
                    {
                        seedProperties[UploadFunctionWizardProperties.Dockerfile] = dockerfilePath;
                        seedProperties[UploadFunctionWizardProperties.PackageType] = Amazon.Lambda.PackageType.Image;
                    }
                }
                else if (File.Exists(fullPath))
                {
                    seedProperties[UploadFunctionWizardProperties.SourcePath] = rootDirectory;
                    if (File.Exists(dockerfilePath))
                    {
                        seedProperties[UploadFunctionWizardProperties.Dockerfile] = dockerfilePath;
                        seedProperties[UploadFunctionWizardProperties.PackageType] = Amazon.Lambda.PackageType.Image;
                    }
                }

                // Look to see if there is a Javascript Lambda function StartupFile and seed that as the suggested function handler.
                var startupFile = selectedProject?.Properties?.Item("StartupFile")?.Value as string;
                if (!string.IsNullOrEmpty(startupFile))
                {
                    string relativePath;
                    if (startupFile.StartsWith(rootDirectory))
                        relativePath = startupFile.Substring(rootDirectory.Length + 1);
                    else
                        relativePath = Path.GetFileName(startupFile);

                    if (!relativePath.StartsWith("_"))
                    {
                        seedProperties[UploadFunctionWizardProperties.Handler] =
                            System.IO.Path.GetFileNameWithoutExtension(relativePath) + ".app.js";
                    }
                }

                // Make sure all open editors are saved before deploying.
                if (!await TrySaveAllDocumentsAsync())
                {
                    Logger.Warn("Unable to save open documents, outdated code might get uploaded to Lambda");
                }

                if (selectedProject != null)
                {
                    var projectFrameworks = VSUtility.GetSelectedNetCoreProjectFrameworks();
                    if (projectFrameworks != null && projectFrameworks.Count > 0)
                    {
                        seedProperties[UploadFunctionWizardProperties.ProjectTargetFrameworks] = projectFrameworks;
                    }
                }

                this.LambdaPlugin.UploadFunctionFromPath(seedProperties);
            }
            catch (Exception e)
            {
                Logger.Error("Deploy to Lambda failure", e);
                _toolkitShell.ShowError("Publish to AWS Lambda failure", e.Message);
            }
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs e)
        {
            try
            {
                // Menu cannot be seen unless we know this project can be deployed to Lambda
                menuCommand.Visible = false;

                if (LambdaPlugin == null)
                {
                    return;
                }

                if (IsLambdaProject())
                {
                    menuCommand.Visible = true;
                }
            }
            catch
            {
                // Swallow error for stability -- menu will not be visible
                // do not log - this is invoked each time the menu is opened
            }
        }

        private bool IsLambdaProject()
        {
            try
            {
                if (VSUtility.IsLambdaDotnetProject)
                {
                    return true;
                }

                if (VSUtility.IsLambdaServerlessProject())
                {
                    return true;
                }

                if (VSUtility.IsLambdaNodeJsProject(_monitorSelection))
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

        private async Task<bool> TrySaveAllDocumentsAsync()
        {
            try
            {
                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!(await Package.GetServiceAsync(typeof(EnvDTE.DTE)) is DTE dte))
                {
                    return false;
                }

                dte.Documents.SaveAll();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn("Error while saving all open documents", ex);
                return false;
            }
        }
    }
}
