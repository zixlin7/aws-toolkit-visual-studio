using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    public interface IContextCommand
    {
        ActionResults Execute(IViewModel model);
    }
}
