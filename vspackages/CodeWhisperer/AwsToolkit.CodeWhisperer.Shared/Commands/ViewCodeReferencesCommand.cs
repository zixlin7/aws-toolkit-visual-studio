using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class ViewCodeReferencesCommand : BaseCommand
    {
        public ViewCodeReferencesCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Display code references
            return Task.CompletedTask;
        }
    }
}
