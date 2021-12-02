using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.VisualStudio
{
    public class DialogFactory : IDialogFactory
    {
        private readonly ToolkitContext _toolkitContext;

        public DialogFactory(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        // todo : implement dialog creation methods
    }
}
