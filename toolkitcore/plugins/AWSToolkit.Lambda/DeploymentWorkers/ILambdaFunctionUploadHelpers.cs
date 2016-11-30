using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Lambda.DeploymentWorkers;
using Amazon.AWSToolkit.Account;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using Amazon.Lambda.Model;
using Amazon.Lambda;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public interface ILambdaFunctionUploadHelpers
    {
        string CreateRole(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, string functionName, IdentityManagement.Model.ManagedPolicy managedPolicy);

        void UploadFunctionAsyncCompleteSuccess(UploadFunctionState uploadState);

        void PublishServerlessAsyncCompleteSuccess(PublishServerlessApplicationWorkerSettings settings);

        void UploadFunctionAsyncCompleteError(string message);

        void AppendUploadStatus(string message, params object[] tokens);

        GetFunctionConfigurationResponse GetExistingConfiguration(IAmazonLambda lambdaClient, string functionName);
    }
}
