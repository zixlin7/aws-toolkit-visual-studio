﻿using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using Amazon.Lambda.Model;
using Amazon.Lambda;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public interface ILambdaFunctionUploadHelpers
    {
        string CreateRole(AccountViewModel account, ToolkitRegion region, string functionName, IdentityManagement.Model.ManagedPolicy managedPolicy);

        void UploadFunctionAsyncCompleteSuccess(UploadFunctionState uploadState);

        void PublishServerlessAsyncCompleteSuccess(PublishServerlessApplicationWorkerSettings settings);

        void UploadFunctionAsyncCompleteError(string message);

        void AppendUploadStatus(string message, params object[] tokens);

        GetFunctionConfigurationResponse GetExistingConfiguration(IAmazonLambda lambdaClient, string functionName);

        void WaitForUpdatableState(IAmazonLambda lambdaClient, string functionName);

        bool XRayEnabled();

        string GetFunctionLanguage();
    }
}
