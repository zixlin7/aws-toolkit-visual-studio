using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    // TODO Consider renaming to something other than "Getting Started" to not conflate with the
    // general Getting Started classes of the toolkit.
    public class GettingStartedCommand : BaseCommand
    {
        public GettingStartedCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Display CodeWhisperer Getting Started UI
            return Task.CompletedTask;
        }
    }
}
