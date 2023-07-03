using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.Commands
{
    public class OpenAwsExplorerCommand : AsyncCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public OpenAwsExplorerCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            return _toolkitContext.ToolkitHost.OpenShellWindowAsync(Shared.ShellWindows.AwsExplorer);
        }
    }
}
