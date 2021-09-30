namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class ServerlessNETCoreFunctionFSharpWizard : BaseNetCoreMsbuildWizard
    {
        public static readonly string[] REQUIRED_TAGS = new string[] { "F#", "ServerlessProject" };
        public const string TITLE = "New AWS Serverless Application";
        public const string DESCRIPTION = "Choose the contents of the F# AWS Serverless application.";

        public override string Title => TITLE;

        public override string ProjectType => "ServerlessProject-Msbuild";
        public override string ProjectLanguage => "F#";

        public override string Description => DESCRIPTION;

        public override string[] RequiredTags => REQUIRED_TAGS;

        public override bool CreateTestProject => false;
    }
}
