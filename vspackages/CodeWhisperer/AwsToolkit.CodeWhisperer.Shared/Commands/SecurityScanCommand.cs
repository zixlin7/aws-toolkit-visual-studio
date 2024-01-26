using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class SecurityScanCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;
        private readonly ICodeWhispererTextView _textView;
        public SecurityScanCommand(ICodeWhispererManager manager, ICodeWhispererTextView textView, IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;
            _textView = textView;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return base.CanExecuteCore(parameter) && _manager.ClientStatus == LspClientStatus.Running && _manager.ConnectionStatus == ConnectionStatus.Connected && !string.IsNullOrWhiteSpace(_textView.GetFilePath());
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await _manager.ScanAsync();
        }
    }
}
