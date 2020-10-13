using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public interface IEcsDeploy
    {
        Task<bool> Deploy(EcsDeployState state);
    }
}