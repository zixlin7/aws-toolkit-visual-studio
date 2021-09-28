namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class ServerlessNETCoreFunctionWithTestsFSharpWizard : ServerlessNETCoreFunctionFSharpWizard
    {
        public override string ProjectType => "ServerlessProject-Tests-Msbuild";

        public override bool CreateTestProject => true;
    }
}
