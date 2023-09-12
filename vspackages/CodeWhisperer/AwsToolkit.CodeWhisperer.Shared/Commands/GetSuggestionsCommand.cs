using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class GetSuggestionsCommand : BaseCommand
    {
        public GetSuggestionsCommand(IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
        }

        protected override Task ExecuteCoreAsync(object parameter)
        {
            // TODO : Display code suggestions to user
            return Task.CompletedTask;
        }
    }
}
