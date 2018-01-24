﻿using System;
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

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class NETCoreFunctionFSharpWizard : BaseNetCoreMsbuildWizard
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "F#", "LambdaProject" };
        public const string TITLE = "New AWS Lambda F# Project";
        public const string DESCRIPTION = "Choose the contents of the F# project for your AWS Lambda function.";

        public override string Title => TITLE;

        public override string ProjectType => "LambdaProject-Msbuild";

        public override string Description => DESCRIPTION;

        public override string[] RequiredTags => REQUIRED_TAGS;

        public override bool CreateTestProject => false;
    }
}
