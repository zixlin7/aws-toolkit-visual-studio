namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class ServerlessNETCoreFunctionWithTestsWizard : ServerlessNETCoreFunctionWizard
    {
        public override string ProjectType => "ServerlessProject-Tests-Msbuild";

        public override bool CreateTestProject => true;
    }
}
