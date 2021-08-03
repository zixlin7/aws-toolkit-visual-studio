using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Lambda;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.VsSdk.Common;

using EnvDTE;

using log4net;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Lambda
{
    /// <summary>
    /// Extension command responsible for adding a serverless.template file
    /// to Lambda projects.
    /// </summary>
    public class AddServerlessTemplateCommand : BaseCommand<AddServerlessTemplateCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(AddServerlessTemplateCommand));

        private readonly IAWSToolkitShellProvider _toolkitShell;

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

        public AddServerlessTemplateCommand(IAWSToolkitShellProvider toolkitShell)
        {
            _toolkitShell = toolkitShell;
        }

        public static Task<AddServerlessTemplateCommand> InitializeAsync(
            IAWSToolkitShellProvider toolkitShell,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new AddServerlessTemplateCommand(toolkitShell),
                menuGroup, commandId,
                package);
        }

        /// <summary>
        /// Writes a serverless.template file to the selected project.
        /// </summary>
        protected override async Task ExecuteAsync(object sender, OleMenuCmdEventArgs args)
        {
            try
            {
                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                var projectLocation = VSUtility.GetSelectedProjectLocation();
                if (string.IsNullOrEmpty(projectLocation))
                {
                    return;
                }

                var destinationFile = Path.Combine(projectLocation, Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME);
                using (var stream = new StreamReader(this.GetType().Assembly
                    .GetManifestResourceStream("Amazon.AWSToolkit.VisualStudio.Resources.basic-serverless.template")))
                using (var outputStream = new StreamWriter(File.Open(destinationFile, FileMode.Create)))
                {
                    await outputStream.WriteAsync(await stream.ReadToEndAsync());
                }

                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (await Package.GetServiceAsync(typeof(DTE)) is DTE dte)
                {
                    dte.ItemOperations.OpenFile(destinationFile);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error creating {Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME}", e);
                _toolkitShell.ShowError("Failed to create file", e.Message);
            }
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs e)
        {
            try
            {
                // Menu cannot be seen unless we know this command is relevant
                menuCommand.Visible = false;

                if (LambdaPlugin == null)
                {
                    return;
                }

                if (VSUtility.IsLambdaDotnetProject)
                {
                    var projectLocation = VSUtility.GetSelectedProjectLocation();
                    var serverlessTemplatePath = Path.Combine(
                        projectLocation,
                        Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME);

                    if (!File.Exists(serverlessTemplatePath))
                    {
                        menuCommand.Visible = true;
                    }
                }
            }
            catch
            {
                // Swallow error for stability -- menu will not be visible
                // do not log - this is invoked each time the menu is opened
            }
        }
    }
}
