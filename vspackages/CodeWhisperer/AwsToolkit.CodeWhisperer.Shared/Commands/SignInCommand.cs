using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class SignInCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;

        public SignInCommand(ICodeWhispererManager manager, IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return _manager.ConnectionStatus == ConnectionStatus.Disconnected && base.CanExecuteCore(parameter);
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await _manager.SignInAsync();
        }
    }
}
