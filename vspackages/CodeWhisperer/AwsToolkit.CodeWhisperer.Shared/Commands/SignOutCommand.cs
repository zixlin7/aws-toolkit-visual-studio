using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class SignOutCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;

        public SignOutCommand(ICodeWhispererManager manager, IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return _manager.GetStatus() == ConnectionStatus.Connected && base.CanExecuteCore(parameter);
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await _manager.SignOutAsync();
        }
    }
}
