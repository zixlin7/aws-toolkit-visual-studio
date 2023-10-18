using System.Windows.Markup;

[assembly:XmlnsPrefix(AssemblyInfo.Xmlns, "vstk")]
[assembly:XmlnsDefinition(AssemblyInfo.Xmlns, "Amazon.AwsToolkit.VsSdk.Common.CommonUI")]
[assembly:XmlnsDefinition(AssemblyInfo.Xmlns, "AwsToolkit.VsSdk.Common.CommonUI.Behaviors")]

internal static class AssemblyInfo
{
    public const string Xmlns = "http://schemas.amazon.com/visualstudiotoolkit/2023";
}
