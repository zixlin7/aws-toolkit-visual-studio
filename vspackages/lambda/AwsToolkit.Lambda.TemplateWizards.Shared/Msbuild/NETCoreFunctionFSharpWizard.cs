namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class NETCoreFunctionFSharpWizard : BaseNetCoreMsbuildWizard
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "F#", "LambdaProject" };
        public const string TITLE = "New AWS Lambda F# Project";
        public const string DESCRIPTION = "Choose the contents of the F# project for your AWS Lambda function.";

        public override string Title => TITLE;

        public override string ProjectType => "LambdaProject-Msbuild";
        public override string ProjectLanguage => "F#"; 

        public override string Description => DESCRIPTION;

        public override string[] RequiredTags => REQUIRED_TAGS;

        public override bool CreateTestProject => false;
    }
}
