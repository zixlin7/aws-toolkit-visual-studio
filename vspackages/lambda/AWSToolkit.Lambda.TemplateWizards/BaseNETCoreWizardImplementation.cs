using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

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
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Lambda.TemplateWizards.Model;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards
{
    public abstract class BaseNETCoreWizardImplementation : IWizard
    {
        string _rootDirectory;
        FileSystemWatcher _watcher;

        // This method is called before opening any item that
        // has the OpenInEditor attribute.
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        // This method is only called for item templates,
        // not for project templates.
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        int _projectJsonFilesCreated;
        int _projectLockJsonFilesCreated;
        public void RunFinished()
        {
            if (!string.IsNullOrEmpty(_rootDirectory))
            {
                this._projectJsonFilesCreated = Directory.GetFiles(_rootDirectory, "project.json", SearchOption.AllDirectories).Length;
                this._watcher = new FileSystemWatcher(_rootDirectory, @"*project.lock.json");
                this._watcher.Created += _watcher_Created;
                this._watcher.IncludeSubdirectories = true;
                this._watcher.EnableRaisingEvents = true;
            }
        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            this._projectLockJsonFilesCreated++;
            if (this._projectJsonFilesCreated == this._projectLockJsonFilesCreated)
            {
                this._watcher.EnableRaisingEvents = false;
                this._watcher.Dispose();
            }

            // Sleep a little to let the restore VS kicked off to finish up.
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
            Restore(this._rootDirectory);
        }

        public abstract string Title {get;}
        public abstract string Description { get; }
        public abstract string[] RequiredTags { get; }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Lambda.View.NewAWSLambdaProject", new Dictionary<string, object>());
                wizard.Title = this.Title;

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new CSharpProjectTypeController("Select Blueprint", this.Description, this.RequiredTags)
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }

                _rootDirectory = replacementsDictionary["$destinationdirectory$"];
                var blueprint = wizard.CollectedProperties[CSharpWizardPropertyNameConstants.propKey_SelectedBlueprint] as Blueprint;

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.LambdaNETCoreNewProjectType, this.ProjectType);
                evnt.AddProperty(AttributeKeys.LambdaNETCoreNewProject, blueprint.Name);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                CreateFromSampleFunction(replacementsDictionary, blueprint.File);
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

        public abstract string ProjectType { get; }

        protected abstract void ApplyBlueprint(Dictionary<string, string> replacementsDictionary, ZipFile blueprintZipFile);

        private void CreateFromSampleFunction(Dictionary<string, string> replacementsDictionary, string sampleFile)
        {
            using (var stream = S3FileFetcher.Instance.OpenFileStream(Path.Combine(BlueprintsManifest.BlueprintsManifestPath, sampleFile), S3FileFetcher.CacheMode.PerInstance))
            using (var zipStream = new ZipInputStream(stream))
            {
                var localZipFile = Path.GetTempFileName() + ".zip";
                using (Stream output = File.OpenWrite(localZipFile))
                    stream.CopyTo(output);

                ZipFile zipFile = new ZipFile(localZipFile);

                ApplyBlueprint(replacementsDictionary, zipFile);


                foreach (var file in Directory.GetFiles(replacementsDictionary["$destinationdirectory$"], "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var originalContent = File.ReadAllText(file);
                        var replacedContent = originalContent.Replace("BLUEPRINT_BASE_NAME", replacementsDictionary["$safeprojectname$"]);
                        if (!string.Equals(originalContent, replacedContent))
                        {
                            File.WriteAllText(file, replacedContent);
                        }
                    }
                    catch
                    { }
                }
            }
        }

        public string Restore(string projectLocation)
        {
            var dotnetCLI = Utility.FindExecutableInPath("dotnet.exe");
            if (string.IsNullOrEmpty(dotnetCLI))
                throw new Exception("Failed to locate dotnet.exe. Make sure the dotnet CLI is installed in the environment PATH.");

            var psi = new ProcessStartInfo
            {
                FileName = dotnetCLI,
                Arguments = string.Format("restore"),
                WorkingDirectory = projectLocation,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = new System.Diagnostics.Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                var outputLog = new StringBuilder();
                var handler = (DataReceivedEventHandler)((o, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;
                    outputLog.AppendLine("......publish: " + e.Data);
                });

                proc.ErrorDataReceived += handler;
                proc.OutputDataReceived += handler;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.EnableRaisingEvents = true;

                proc.WaitForExit();
                outputLog.AppendLine($"......publish: Exit Code {proc.ExitCode}");
                return outputLog.ToString();
            }
        }
    }
}
