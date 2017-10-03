using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class LaunchClusterController :  BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            return new ActionResults().WithSuccess(true);
        }
    }
}
