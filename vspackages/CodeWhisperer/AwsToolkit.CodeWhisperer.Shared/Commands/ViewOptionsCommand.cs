using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

using Community.VisualStudio.Toolkit;

using Microsoft.VisualStudio;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    /// <summary>
    /// Displays the Visual Studio Options dialog, opened to the CodeWhisperer settings.
    /// </summary>
    public class ViewOptionsCommand : BaseCommand
    {
        public ViewOptionsCommand(IToolkitContextProvider toolkitContextProvider) : base(toolkitContextProvider)
        {
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await VS.Commands.ExecuteAsync(VSConstants.VSStd97CmdID.ToolsOptions, typeof(CodeWhispererSettingsProvider).GUID.ToString());
        }
    }
}
