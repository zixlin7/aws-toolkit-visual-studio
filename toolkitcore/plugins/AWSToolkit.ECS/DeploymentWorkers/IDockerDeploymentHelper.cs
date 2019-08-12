namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public interface IDockerDeploymentHelper
    {
        void AppendUploadStatus(string message, params object[] tokens);

        void SendCompleteSuccessAsync(PushImageToECRWorker.State state);

        void SendCompleteSuccessAsync(DeployServiceWorker.State state);
        void SendCompleteSuccessAsync(DeployScheduleTaskWorker.State state);
        void SendCompleteSuccessAsync(DeployTaskWorker.State state);

        void SendCompleteErrorAsync(string message);
    }
}
