using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class SecurityScanCommand : BaseCommand
    {
        public SecurityScanCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Perform a CodeWhisperer security scan
            return Task.CompletedTask;
        }
    }
}
