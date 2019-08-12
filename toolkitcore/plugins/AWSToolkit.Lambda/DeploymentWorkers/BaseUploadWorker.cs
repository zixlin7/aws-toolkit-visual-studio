using Amazon.AWSToolkit.Navigator;
using Amazon.Lambda;

using Amazon.AWSToolkit.Lambda.Controller;

namespace Amazon.AWSToolkit.Lambda.DeploymentWorkers
{
    public abstract class BaseUploadWorker
    {
        protected ILambdaFunctionUploadHelpers FunctionUploader { get; }
        protected IAmazonLambda LambdaClient { get; }

        public ActionResults Results { get; protected set; }

        public BaseUploadWorker(ILambdaFunctionUploadHelpers functionUploader, IAmazonLambda lambdaClient)
        {
            this.FunctionUploader = functionUploader;
            this.LambdaClient = lambdaClient;
        }

        public abstract void UploadFunction(UploadFunctionController.UploadFunctionState uploadState);

        public string CreateRole(UploadFunctionController.UploadFunctionState uploadState)
        {
            return this.FunctionUploader.CreateRole(uploadState.Account, uploadState.Region, uploadState.Request.FunctionName, uploadState.SelectedManagedPolicy);
        }
    }
}
