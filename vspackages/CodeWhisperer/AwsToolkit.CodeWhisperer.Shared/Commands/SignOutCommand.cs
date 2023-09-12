using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class SignOutCommand : BaseCommand
    {
        public SignOutCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Sign Out
            return Task.CompletedTask;
        }
    }
}
