using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Microsoft.Win32;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages;
using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageControllers;
using Amazon.AWSToolkit.SimpleWorkers;

using ICSharpCode.SharpZipLib.Zip;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards
{
    public class ProjectWizardImplementation : IWizard
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

        private bool IsNodeJSPluginIsInstalled(object automationObject)
        {
            if(!(automationObject is DTE))
                return true;

            DTE dte = (DTE)automationObject;

            try
            {
                var path = dte.Solution.TemplatePath["{3af33f2e-1136-4d97-bbb7-1795711ac8b8}"];
                return path != null;
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

            if (!this.IsNodeJSPluginIsInstalled(automationObject))
            {
                string msg = string.Format("Before using this project template the Node.JS Tools for Visual Studio plugin must be installed.\r\n\r\n<a href=\"{0}\">{0}</a>", "http://nodejstools.codeplex.com/");
                ToolkitFactory.Instance.ShellProvider.ShowErrorWithLinks("Missing Node.JS Plugin", msg);
                throw new WizardCancelledException("Missing Node.JS plugin");
            }

            try
            {
                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("New AWS Lambda Project", new Dictionary<string, object>());
                wizard.Title = "New AWS Lambda Project";

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new ProjectTypeController("Select Project Source", "Choose the source for the Lambda function created with the new project.")
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }

                var creationMode = (ProjectTypeController.CreationMode)wizard.CollectedProperties[WizardPropertyNameConstants.propKey_CreationMode];

                switch (creationMode)
                {
                    case ProjectTypeController.CreationMode.ExistingFunction:
                        CreateFromExistingFunction(replacementsDictionary, wizard);
                        break;
                    case ProjectTypeController.CreationMode.FromSample:
                        CreateFromSampleFunction(replacementsDictionary, wizard);
                        break;
                    default:
                        CreateBasicProjectStartup(replacementsDictionary, wizard);
                        break;
                }

            }
            catch (WizardCancelledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(ex.ToString());
            }
        }

        private void CreateBasicProjectStartup(Dictionary<string, string> replacementsDictionary, IAWSWizard wizard)
        {
            var projectFolder = replacementsDictionary["$destinationdirectory$"] as string;

            using (var stream = this.GetType().Assembly.GetManifestResourceStream("AWSToolkit.Lambda.TemplateWizards.Resources.basic-startupfiles.zip"))
            {
                CreateFromStream(replacementsDictionary, stream);
            }
        }


        private void CreateFromExistingFunction(Dictionary<string, string> replacementsDictionary, IAWSWizard wizard)
        {
            var account = wizard.CollectedProperties[WizardPropertyNameConstants.propKey_SelectedAccount] as AccountViewModel;
            var region = wizard.CollectedProperties[WizardPropertyNameConstants.propKey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
            var existingFunctionName = wizard.CollectedProperties[WizardPropertyNameConstants.propKey_ExistingFunctionName] as string;

            var client = account.CreateServiceClient<AmazonLambdaClient>(region);

            var response = client.GetFunction(existingFunctionName);
            CreateFromUrl(replacementsDictionary, response.Code.Location);
        }

        private void CreateFromSampleFunction(Dictionary<string, string> replacementsDictionary, IAWSWizard wizard)
        {
            var sample = wizard.CollectedProperties[WizardPropertyNameConstants.propKey_SampleFunction] as QueryLambdaFunctionSamplesWorker.SampleSummary;

            using(var stream = S3FileFetcher.Instance.OpenFileStream(sample.File, S3FileFetcher.CacheMode.PerInstance))
            {
                CreateFromStream(replacementsDictionary, stream);
            }
        }

        private void CreateFromStream(Dictionary<string, string> replacementsDictionary, Stream stream)
        {
            var localZipFile = Path.GetTempFileName() + ".zip";
            using (Stream output = File.OpenWrite(localZipFile))
                stream.CopyTo(output);

            try
            {
                CreateFromZipFile(replacementsDictionary, localZipFile);
            }
            finally
            {
                File.Delete(localZipFile);
            }
        }

        private void CreateFromUrl(Dictionary<string, string> replacementsDictionary, string url)
        {
            var localZipFile = Path.GetTempFileName() + ".zip";
            WebClient client = new WebClient();
            client.DownloadFile(new Uri(url), localZipFile);
            try
            {
                CreateFromZipFile(replacementsDictionary, localZipFile);
            }
            finally
            {
                File.Delete(localZipFile);
            }
        }

        private void CreateFromZipFile(Dictionary<string, string> replacementsDictionary, string zipFile)
        {
            var projectFolder = replacementsDictionary["$destinationdirectory$"] as string;

            var zip = new FastZip();
            zip.ExtractZip(zipFile, projectFolder, FastZip.Overwrite.Always, null, null, null, true);
        }
    }
}
