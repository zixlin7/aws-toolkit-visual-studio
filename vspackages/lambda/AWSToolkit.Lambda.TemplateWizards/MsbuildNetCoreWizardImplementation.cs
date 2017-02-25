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

using log4net;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards
{
    public class MsbuildNetCoreWizardImplementation : IWizard
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(MsbuildNetCoreWizardImplementation));

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
            try
            {
                var srcDirInfo = new DirectoryInfo(Path.GetDirectoryName(project.FullName));
                var targetDirInfo = new DirectoryInfo(Path.Combine(srcDirInfo.Parent.FullName, srcDirInfo.Name + ".Tests"));

                if (targetDirInfo.Exists)
                    return;

                targetDirInfo.Create();

                var testProjectPath = $"{targetDirInfo.FullName}\\{targetDirInfo.Name}.csproj";

                File.Copy(@"C:\codebase\aws-lambda-dotnet\Blueprints\BlueprintDefinitions\Msbuild\EmptyFunction\template\test\BlueprintBaseName.Tests\BlueprintBaseName.Tests.csproj",
                    testProjectPath);

                this._dte.Solution.AddFromFile(testProjectPath);
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

        public string Title { get; } = "The Title";
        public string Description { get; } = "The Description";
        public string[] RequiredTags { get; }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        DTE _dte;
        Dictionary<string, string> _replacementsDictionary

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                this._dte = (DTE)automationObject;
                this._replacementsDictionary = replacementsDictionary;
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
