namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class NETCoreFunctionWithTestsWizard : NETCoreFunctionWizard
    {
        public override string ProjectType => "LambdaProject-Tests-Msbuild";

        public override bool CreateTestProject => true;
    }
}
