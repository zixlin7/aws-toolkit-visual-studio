﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages;
using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageControllers;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.Lambda;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards
{
    public class NodeProjectWizardImplementation : IWizard
    {
        ProjectItem _startupFile;

        // This method is called before opening any item that
        // has the OpenInEditor attribute.
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
            var projectDirectory = Directory.GetParent(project.FullName);
            FileInfo[] files = projectDirectory.GetFiles("*", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                if (string.Equals(file.Extension, ".njsproj", StringComparison.InvariantCultureIgnoreCase) || file.FullName.Contains("node_modules") || file.Name.StartsWith("."))
                    continue;

                project.ProjectItems.AddFromFile(file.FullName);
            }

            var jsFiles = Directory.GetFiles(projectDirectory.FullName, "*.js", SearchOption.TopDirectoryOnly);

            string startupFile = null;
            if (jsFiles.Any(x => x.EndsWith("_testdriver.js", StringComparison.InvariantCultureIgnoreCase)))
                startupFile = "_testdriver.js";
            else if (jsFiles.Any(x => x.EndsWith("app.js", StringComparison.InvariantCultureIgnoreCase)))
                startupFile = "app.js";
            else if (jsFiles.Length == 1)
                startupFile = Path.GetFileName(jsFiles[0]);

            if (startupFile != null)
            {
                project.Properties.Item("StartupFile").Value = startupFile;

                this._startupFile = project.ProjectItems.Item(startupFile);
            }
        }

        // This method is only called for item templates,
        // not for project templates.
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        // This method is called after the project is created.
        public void RunFinished()
        {
            if(this._startupFile != null)
            {
                var window = this._startupFile.Open(VSConstants.LOGVIEWID_Primary.ToString());
                if (window != null)
                {
                    window.Visible = true;
                }

                this._startupFile = null;
            }
        }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        private bool IsNodeJSPluginIsInstalled()
        {
            try
            {
                return (new SetupConfiguration().GetInstanceForCurrentProcess() as ISetupInstance2)?.GetPackages()
                    .Any(p => p.GetId() == "Microsoft.VisualStudio.Workload.Node") == true;
            }
            catch
            {
                return false;
            }
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            var result = Result.Failed;
            string blueprintName = null;

            if (!IsNodeJSPluginIsInstalled())
            {
                string msg = string.Format("The Node.js development workload must be installed to use this project template.\r\n\r\n<a href=\"{0}\">{0}</a>", "https://docs.microsoft.com/en-us/visualstudio/install/modify-visual-studio");
                ToolkitFactory.Instance.ShellProvider.ShowErrorWithLinks("Missing Node.js development workload", msg);
                throw new WizardCancelledException("Missing Node.js development workload");
            }

            try
            {
                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard(
                    "Amazon.AWSToolkit.Lambda.View.NewAWSLambdaProject", new Dictionary<string, object>());
                wizard.Title = "New AWS Lambda Node.js Project";

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new NodeProjectTypeController("Select Project Source",
                        "Choose the source for the Lambda function created with the new project.")
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }

                var creationMode =
                    (NodeProjectTypeController.CreationMode) wizard.CollectedProperties[
                        NodeWizardPropertyNameConstants.propKey_CreationMode];

                if (creationMode == NodeProjectTypeController.CreationMode.FromSample)
                {
                    if (wizard.CollectedProperties[NodeWizardPropertyNameConstants.propKey_SampleFunction]
                        is QueryLambdaFunctionSamplesWorker.SampleSummary sample)
                    {
                        blueprintName = $"{creationMode}/{sample.File}";
                    }
                }
                else
                {
                    blueprintName = creationMode.ToString();
                }


                switch (creationMode)
                {
                    case NodeProjectTypeController.CreationMode.ExistingFunction:
                        CreateFromExistingFunction(replacementsDictionary, wizard);
                        break;
                    case NodeProjectTypeController.CreationMode.FromSample:
                        CreateFromSampleFunction(replacementsDictionary, wizard);
                        break;
                    default:
                        CreateBasicProjectStartup(replacementsDictionary, wizard);
                        break;
                }

                result = Result.Succeeded;
            }
            catch (WizardCancelledException)
            {
                result = Result.Cancelled;
                throw;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(ex.ToString());
            }
            finally
            {
                RecordLambdaCreateProjectMetric(result, blueprintName);
            }
        }


        private void CreateBasicProjectStartup(Dictionary<string, string> replacementsDictionary, IAWSWizard wizard)
        {
            var projectFolder = replacementsDictionary["$destinationdirectory$"] as string;

            using (var stream = this.GetType().Assembly.GetManifestResourceStream("Amazon.AWSToolkit.Lambda.TemplateWizards.Resources.basic-startupfiles.zip"))
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, stream, null);
            }
        }

        private void CreateFromExistingFunction(Dictionary<string, string> replacementsDictionary, IAWSWizard wizard)
        {
            var account = wizard.CollectedProperties[NodeWizardPropertyNameConstants.propKey_SelectedAccount] as AccountViewModel;
            var region = wizard.CollectedProperties[NodeWizardPropertyNameConstants.propKey_SelectedRegion] as ToolkitRegion;
            var existingFunctionName = wizard.CollectedProperties[NodeWizardPropertyNameConstants.propKey_ExistingFunctionName] as string;

            var client = account.CreateServiceClient<AmazonLambdaClient>(region);

            var response = client.GetFunction(existingFunctionName);
            ProjectCreatorUtilities.CreateFromUrl(replacementsDictionary, response.Code.Location, null);
        }

        private void CreateFromSampleFunction(Dictionary<string, string> replacementsDictionary, IAWSWizard wizard)
        {
            var sample = wizard.CollectedProperties[NodeWizardPropertyNameConstants.propKey_SampleFunction] as QueryLambdaFunctionSamplesWorker.SampleSummary;

            using(var stream = S3FileFetcher.Instance.OpenFileStream(sample.File, S3FileFetcher.CacheMode.PerInstance))
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, stream, null);
            }
        }

        private static void RecordLambdaCreateProjectMetric(Result result, string blueprintName)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordLambdaCreateProject(new LambdaCreateProject
            {
                AwsAccount = ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = ToolkitFactory.Instance.AwsConnectionManager.ActiveRegion?.Id ?? MetadataValue.NotSet,
                Result = result,
                TemplateName = blueprintName,
                Language = "NodeJS"
            });
        }
    }
}
