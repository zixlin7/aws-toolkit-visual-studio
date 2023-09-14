using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    /// <summary>
    /// Base class for CodeWhisperer Async ICommands
    /// </summary>
    public abstract class BaseCommand : AsyncCommand
    {
        protected readonly IToolkitContextProvider _toolkitContextProvider;

        public BaseCommand(IToolkitContextProvider toolkitContextProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;
        }

        protected override bool CanExecuteCore(object parameter)
        {
            return _toolkitContextProvider.HasToolkitContext() && base.CanExecuteCore(parameter);
        }
    }
}
