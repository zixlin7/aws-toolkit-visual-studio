using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class ViewCodeReferencesCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;

        public ViewCodeReferencesCommand(ICodeWhispererManager manager,
            IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await _toolkitContextProvider.GetToolkitContext().ToolkitHost.OpenShellWindowAsync(ShellWindows.Output);
            await _manager.ShowReferenceLoggerAsync();
        }
    }
}
