namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class NETCoreFunctionWizard : BaseNetCoreMsbuildWizard
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "C#", "LambdaProject" };
        public const string TITLE = "New AWS Lambda C# Project";
        public const string DESCRIPTION = "Choose the contents of the C# project for your AWS Lambda function.";

        public override string Title => TITLE;

        public override string ProjectType => "LambdaProject-Msbuild";
        public override string ProjectLanguage => "C#";

        public override string Description => DESCRIPTION;

        public override string[] RequiredTags => REQUIRED_TAGS;

        public override bool CreateTestProject => false;
    }
}
