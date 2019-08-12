using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.ProjectJson
{
    public class NETCoreProjectWizardImplementation : BaseNETCoreWizardImplementation
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "C#", "LambdaProject" };
        public const string TITLE = "New AWS Lambda C# Project";
        public const string DESCRIPTION = "Choose the contents of the C# project for your AWS Lambda function.";

        public override string Title => TITLE;

        public override string ProjectType => "LambdaProject";

        public override string Description => DESCRIPTION;

        public override string[] RequiredTags => REQUIRED_TAGS;

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
