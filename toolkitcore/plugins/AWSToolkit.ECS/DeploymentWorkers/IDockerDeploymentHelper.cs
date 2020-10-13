namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public interface IDockerDeploymentHelper
    {
        void AppendUploadStatus(string message, params object[] tokens);

        void SendImagePushCompleteSuccessAsync(EcsDeployState state);

        void SendCompleteSuccessAsync(EcsDeployState state);

        void SendCompleteErrorAsync(string message);
    }
}
