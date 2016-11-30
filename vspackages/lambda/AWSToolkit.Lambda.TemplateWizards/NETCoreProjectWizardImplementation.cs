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
    public class NETCoreProjectWizardImplementation : BaseNETCoreWizardImplementation
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "C#", "LambdaProject" };
        public const string TITLE = "New AWS Lambda C# Project";
        public const string DESCRIPTION = "Choose the contents of the C# project for your AWS Lambda function.";

        public override string Title
        {
            get
            {
                return TITLE;
            }
        }

        public override string ProjectType
        {
            get
            {
                return "LambdaProject";
            }
        }

        public override string Description
        {
            get
            {
                return DESCRIPTION;
            }
        }

        public override string[] RequiredTags
        {
            get { return REQUIRED_TAGS; }
        }

        protected override void ApplyBlueprint(Dictionary<string, string> replacementsDictionary, ZipFile blueprintZipFile)
        {
            ZipEntry srcEntry = blueprintZipFile.GetEntry("src.zip");
            if (srcEntry != null)
            {
                ProjectCreatorUtilities.CreateFromStream(replacementsDictionary, blueprintZipFile.GetInputStream(srcEntry), null);
            }
        }
    }
}
