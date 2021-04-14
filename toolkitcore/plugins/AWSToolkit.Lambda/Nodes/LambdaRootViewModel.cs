using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public class LambdaRootViewModel : ServiceRootViewModel, ILambdaRootViewModel
    {
        private readonly LambdaRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonLambda> _lambdaClient;

        public LambdaRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild < LambdaRootViewMetaNode>(), accountViewModel, "AWS Lambda", region)
        {
            _metaNode = base.MetaNode as LambdaRootViewMetaNode;
            _lambdaClient = new Lazy<IAmazonLambda>(CreateLambdaClient);
        }

        public override string ToolTip => "AWS Lambda is a compute service that runs your code in response to events and automatically manages the compute resources.";

        protected override string IconName => "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.service-root.png";

        public IAmazonLambda LambdaClient => this._lambdaClient.Value;

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
                SetChildren(items);
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

        private IAmazonLambda CreateLambdaClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonLambdaClient>(Region);
        }
    }
}
