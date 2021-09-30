namespace Amazon.AWSToolkit.Lambda.TemplateWizards.Msbuild
{
    public class NETCoreFunctionWithTestsFSharpWizard : NETCoreFunctionFSharpWizard
    {
        public override string ProjectType => "LambdaProject-Tests-Msbuild";

        public override bool CreateTestProject => true;
    }
}
