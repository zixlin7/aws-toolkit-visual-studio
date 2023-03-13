using System.Threading.Tasks;

using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public interface IEcsDeploy
    {
        Task<ActionResults> Deploy(EcsDeployState state);
    }
}
