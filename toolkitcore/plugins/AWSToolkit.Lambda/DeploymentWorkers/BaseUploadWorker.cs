using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Navigator;
using Amazon.ECR;
using Amazon.Lambda;
using Amazon.SecurityToken;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public abstract class BaseUploadWorker
    {
        protected ILambdaFunctionUploadHelpers FunctionUploader { get; }
        protected IAmazonSecurityTokenService StsClient { get; }
        protected IAmazonLambda LambdaClient { get; }
        protected IAmazonECR ECRClient { get; }

        public ActionResults Results { get; protected set; }

        public BaseUploadWorker(ILambdaFunctionUploadHelpers functionUploader, IAmazonSecurityTokenService stsClient, IAmazonLambda lambdaClient, IAmazonECR ecrClient)
        {
            this.FunctionUploader = functionUploader;
            this.StsClient = stsClient;
            this.LambdaClient = lambdaClient;
            this.ECRClient = ecrClient;
        }

        public abstract void UploadFunction(UploadFunctionController.UploadFunctionState uploadState);

        public string CreateRole(UploadFunctionController.UploadFunctionState uploadState)
        {
            return this.FunctionUploader.CreateRole(uploadState.Account, uploadState.Region, uploadState.Request.FunctionName, uploadState.SelectedManagedPolicy);
        }
    }
}
