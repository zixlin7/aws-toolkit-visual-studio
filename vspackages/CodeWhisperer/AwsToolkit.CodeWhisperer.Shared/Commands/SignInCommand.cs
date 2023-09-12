using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class SignInCommand : BaseCommand
    {
        public SignInCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Sign in
            return Task.CompletedTask;
        }
    }
}
