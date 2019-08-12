using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.ProjectJson
{
    public class NETCoreServerlessWizardImplementation : BaseNETCoreWizardImplementation
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "C#", "ServerlessProject" };
        public const string TITLE = "New AWS Serverless Application";
        public const string DESCRIPTION = "Choose the contents of the C# AWS Serverless application.";

        public override string Title => TITLE;

        public override string ProjectType => "ServerlessProject";

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
