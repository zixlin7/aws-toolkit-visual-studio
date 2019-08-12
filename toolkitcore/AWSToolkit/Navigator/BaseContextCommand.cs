using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    public abstract class BaseContextCommand : IContextCommand
    {
        public abstract ActionResults Execute(IViewModel model);
    }
}
