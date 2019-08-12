using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public class LambdaRootViewModel : ServiceRootViewModel, ILambdaRootViewModel
    {
        LambdaRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;
        IAmazonLambda _lambdaClient;

        public LambdaRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild < LambdaRootViewMetaNode>(), accountViewModel, "AWS Lambda")
        {
            this._metaNode = base.MetaNode as LambdaRootViewMetaNode;
            this._accountViewModel = accountViewModel;
        }

        public override string ToolTip => "AWS Lambda is a compute service that runs your code in response to events and automatically manages the compute resources.";

        protected override string IconName => "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.service-root.png";

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            AmazonLambdaConfig config = new AmazonLambdaConfig();
            this.CurrentEndPoint.ApplyToClientConfig(config);
            this._lambdaClient = new AmazonLambdaClient(awsCredentials, config);
        }

        public IAmazonLambda LambdaClient => this._lambdaClient;

        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>();
                ListFunctionsResponse response = new ListFunctionsResponse();
                do
                {
                    var request = new ListFunctionsRequest() { Marker = response.NextMarker };
                    ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                    response = this.LambdaClient.ListFunctions(request);

                    foreach (var function in response.Functions)
                    {
                        var child = new LambdaFunctionViewModel(this._metaNode.LambdaFunctionViewMetaNode, this, function);
                        items.Add(child);
                    }
                } while (!string.IsNullOrEmpty(response.NextMarker));

                items.Sort(new Comparison<IViewModel>(AWSViewModel.CompareViewModel));
                BeginCopingChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        public void AddFunction(FunctionConfiguration configuration)
        {
            var node = new LambdaFunctionViewModel(this._metaNode.LambdaFunctionViewMetaNode, this, configuration);
            base.AddChild(node);
        }


        internal void RemoveFunction(string functionName)
        {
            this.RemoveChild(functionName);
        }
    }
}
