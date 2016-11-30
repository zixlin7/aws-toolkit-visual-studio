using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
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
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Lambda.TemplateWizards.Model;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards
{
    public class NETCoreServerlessWithTestsWizardImplementation : NETCoreServerlessWizardImplementation
    {

        protected override void ApplyBlueprint(Dictionary<string, string> replacementsDictionary, ZipFile blueprintZipFile)
        {
            base.ApplyBlueprint(replacementsDictionary, blueprintZipFile);
            ZipEntry srcEntry = blueprintZipFile.GetEntry("src.zip");
            if (srcEntry != null)
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, blueprintZipFile.GetInputStream(srcEntry), replacementsDictionary["$safeprojectname$"]);
            }

            ZipEntry testEntry = blueprintZipFile.GetEntry("test.zip");
            if (testEntry != null)
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, blueprintZipFile.GetInputStream(testEntry), replacementsDictionary["$safeprojectname$"] + ".Tests");
            }
        }
    }
}
