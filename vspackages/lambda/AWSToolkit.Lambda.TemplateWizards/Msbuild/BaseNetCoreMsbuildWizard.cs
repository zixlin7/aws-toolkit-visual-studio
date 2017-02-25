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

using System.IO.Compression;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Lambda.TemplateWizards.Model;

using log4net;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public abstract class BaseNetCoreMsbuildWizard : IWizard
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(BaseNetCoreMsbuildWizard));

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        private string ProcessProject(ZipArchive zipArchive, DirectoryInfo destinationDirectory, string archiveRoot)
        {
            string projectFile = null;
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {

                if (entry.FullName.StartsWith(archiveRoot))
                {
                    var relativePath = entry.FullName.Substring(archiveRoot.Length);

                    using (var entryStream = entry.Open())
                    {
                        MemoryStream ms = new MemoryStream();
                        entryStream.CopyTo(ms);

                        var fullPath = Path.Combine(destinationDirectory.FullName, relativePath);

                        if (string.Equals(Path.GetExtension(fullPath), ".csproj"))
                        {
                            projectFile = Directory.GetFiles(destinationDirectory.FullName, "*.csproj").FirstOrDefault();
                            if (projectFile == null)
                            {
                                projectFile = Path.Combine(destinationDirectory.FullName, destinationDirectory.Name + ".Tests.csproj");
                            }

                            File.WriteAllBytes(projectFile, ms.ToArray());
                        }
                        else
                        {
                            File.WriteAllBytes(fullPath, ms.ToArray());
                        }
                    }
                }
            }


            foreach (var file in Directory.GetFiles(destinationDirectory.FullName, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var originalContent = File.ReadAllText(file);
                    var replacedContent = originalContent.Replace("BlueprintBaseName", _replacementsDictionary["$safeprojectname$"]);

                    if(file.EndsWith("aws-lambda-tools-defaults.json"))
                    {
                        replacedContent = replacedContent.Replace("DefaultProfile", ToolkitFactory.Instance.Navigator.SelectedAccount.DisplayName);
                        replacedContent = replacedContent.Replace("DefaultRegion", ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints.SystemName);
                    }

                    if (!string.Equals(originalContent, replacedContent))
                    {
                        File.WriteAllText(file, replacedContent);
                    }
                }
                catch
                { }
            }

            return projectFile;
        }

        public void ProjectFinishedGenerating(Project project)
        {
            try
            {
                var srcDirInfo = new DirectoryInfo(Path.GetDirectoryName(project.FullName));

                var localZipFile = Path.GetTempFileName() + ".zip";
                using (var stream = S3FileFetcher.Instance.OpenFileStream(Path.Combine(BlueprintsManifest.BlueprintsManifestPathMsbuildV1, this._blueprint.File), S3FileFetcher.CacheMode.PerInstance))
                {
                    
                    using (Stream output = File.OpenWrite(localZipFile))
                        stream.CopyTo(output);
                }

                using (var stream = File.OpenRead(localZipFile))
                using (var zipArchive = new ZipArchive(stream))
                {
                    ProcessProject(zipArchive, srcDirInfo, "src/BlueprintBaseName/");

                    if (this.CreateTestProject)
                    {
                        var targetDirInfo = new DirectoryInfo(Path.Combine(srcDirInfo.Parent.FullName, srcDirInfo.Name + ".Tests"));

                        if (targetDirInfo.Exists)
                            return;

                        targetDirInfo.Create();

                        var projectFile = ProcessProject(zipArchive, targetDirInfo, "test/BlueprintBaseName.Tests/");

                        this._dte.Solution.AddFromFile(projectFile);
                    }
                }
            }
            catch(Exception e)
            {
                LOGGER.Error("Error creating test project: " + e.Message, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating test project: " + e.Message);
            }
        }

        // This method is only called for item templates,
        // not for project templates.
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {

        }

        public abstract string Title { get; }
        public abstract string Description { get; }
        public abstract string[] RequiredTags { get; }
        public abstract string ProjectType { get; }
        public abstract bool CreateTestProject { get; }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        DTE _dte;
        Dictionary<string, string> _replacementsDictionary;
        Blueprint _blueprint;

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                this._dte = (DTE)automationObject;
                this._replacementsDictionary = replacementsDictionary;

                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Lambda.View.NewAWSLambdaProject", new Dictionary<string, object>());
                wizard.Title = this.Title;

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new CSharpProjectTypeController("Select Blueprint", this.Description, this.RequiredTags, BlueprintsManifest.BlueprintsManifestPathMsbuildV1)
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() != true)
                {
                    throw new WizardCancelledException();
                }

                this._blueprint = wizard.CollectedProperties[CSharpWizardPropertyNameConstants.propKey_SelectedBlueprint] as Blueprint;

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.LambdaNETCoreNewProjectType, this.ProjectType);
                evnt.AddProperty(AttributeKeys.LambdaNETCoreNewProject, this._blueprint.Name);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
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
    }
}
