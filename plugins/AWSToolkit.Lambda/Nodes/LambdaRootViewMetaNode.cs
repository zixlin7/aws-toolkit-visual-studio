using System;
using System.Linq;
using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.Lambda.Model;


namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public class LambdaRootViewMetaNode : ServiceRootViewMetaNode
    {
        public const string LAMBDA_ENDPOINT_LOOKUP = RegionEndPointsManager.LAMBDA_SERVICE_NAME;

        public ActionHandlerWrapper.ActionHandler OnUploadFunction
        {
            get;
            set;
        }

        public void OnUploadFunctionResponse(IViewModel focus, ActionResults results)
        {
            LambdaRootViewModel rootModel = focus as LambdaRootViewModel;
            object function;
            if (results.Parameters.TryGetValue(LambdaContants.PARAM_LAMBDA_FUNCTION, out function) && function is FunctionConfiguration)
            {
                rootModel.AddFunction((FunctionConfiguration)function);
            }
        }


        public override IList<ActionHandlerWrapper> Actions
        {

            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Create New Function", OnUploadFunction, new ActionHandlerWrapper.ActionResponseHandler(this.OnUploadFunctionResponse), false, null, null)
                    );
            }
        }


        public override string EndPointSystemName
        {
            get { return LAMBDA_ENDPOINT_LOOKUP; }
        }

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new LambdaRootViewModel(account);
        }

        public LambdaFunctionViewMetaNode LambdaFunctionViewMetaNode
        {
            get { return this.FindChild<LambdaFunctionViewMetaNode>(); }
        }

        public override string MarketingWebSite
        {
            get
            {
                return "http://aws.amazon.com/lambda/";
            }
        }
    }
}
