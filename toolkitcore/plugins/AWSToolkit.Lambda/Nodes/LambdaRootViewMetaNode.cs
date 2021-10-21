using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Lambda;
using Amazon.Lambda.Model;


namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public class LambdaRootViewMetaNode : ServiceRootViewMetaNode
    {
        private static readonly string LambdaServiceName = new AmazonLambdaConfig().RegionEndpointServiceName;

        public ActionHandlerWrapper.ActionHandler OnUploadFunction
        {
            get;
            set;
        }

        public void OnUploadFunctionResponse(IViewModel focus, ActionResults results)
        {
            LambdaRootViewModel rootModel = focus as LambdaRootViewModel;
            object function;
            if (results.Parameters.TryGetValue(LambdaConstants.PARAM_LAMBDA_FUNCTION, out function) && function is FunctionConfiguration)
            {
                rootModel.AddFunction((FunctionConfiguration)function);
            }
        }


        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create New Function", OnUploadFunction, new ActionHandlerWrapper.ActionResponseHandler(this.OnUploadFunctionResponse), false,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.Lambda.Path)
            );


        public override string SdkEndpointServiceName =>LambdaServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new LambdaRootViewModel(account, region);
        }

        public LambdaFunctionViewMetaNode LambdaFunctionViewMetaNode => this.FindChild<LambdaFunctionViewMetaNode>();

        public override string MarketingWebSite => "https://aws.amazon.com/lambda/";
    }
}
