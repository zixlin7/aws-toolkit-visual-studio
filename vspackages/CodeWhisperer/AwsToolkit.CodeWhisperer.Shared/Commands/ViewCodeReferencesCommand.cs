using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Commands
{
    public class ViewCodeReferencesCommand : BaseCommand
    {
        private readonly ICodeWhispererManager _manager;

        public ViewCodeReferencesCommand(ICodeWhispererManager manager,
            IToolkitContextProvider toolkitContextProvider)
            : base(toolkitContextProvider)
        {
            _manager = manager;
        }

        protected override async Task ExecuteCoreAsync(object parameter)
        {
            await _manager.ShowReferenceLoggerAsync();
        }
    }
}
