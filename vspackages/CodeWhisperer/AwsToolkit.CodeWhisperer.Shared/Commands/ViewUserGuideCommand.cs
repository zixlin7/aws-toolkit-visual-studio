using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class ViewUserGuideCommand : BaseCommand
    {
        public ViewUserGuideCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Display CodeWhisperer User Guide
            return Task.CompletedTask;
        }
    }
}
