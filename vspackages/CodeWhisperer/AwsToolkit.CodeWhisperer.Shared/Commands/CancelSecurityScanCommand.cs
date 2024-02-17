using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class CancelSecurityScanCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;
        public CancelSecurityScanCommand(ICodeWhispererManager manager, IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return base.CanExecuteCore(parameter) && _manager.ClientStatus == LspClientStatus.Running && _manager.ConnectionStatus == ConnectionStatus.Connected
                && _manager.SecurityScanState == SecurityScanState.Running;
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await _manager.CancelScanAsync();
        }
    }
}
