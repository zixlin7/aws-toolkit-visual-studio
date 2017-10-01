using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public interface IDockerDeploymentHelper
    {
        void AppendUploadStatus(string message, params object[] tokens);

        void SendCompleteSuccessAsync(PushImageToECRWorker.State state);

        void SendCompleteErrorAsync(string message);
    }
}
