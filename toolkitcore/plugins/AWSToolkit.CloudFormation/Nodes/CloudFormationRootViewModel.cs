using System.Collections.Generic;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public class CloudFormationRootViewModel : ServiceRootViewModel, ICloudFormationRootViewModel
    {
        CloudFormationRootViewMetaNode _metaNode;
        AccountViewModel _accountViewModel;


        IAmazonCloudFormation _cloudFormationClient;

        public CloudFormationRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<CloudFormationRootViewMetaNode>(), accountViewModel, "AWS CloudFormation")
        {
            this._metaNode = base.MetaNode as CloudFormationRootViewMetaNode;
            this._accountViewModel = accountViewModel;            
        }

        public override string ToolTip => "AWS CloudFormation gives you an easier way to create a collection of related AWS resources (a stack) by describing your requirements in a template.";

        public void RemoveStack(string stackName)
        {
            base.RemoveChild(stackName);
        }

        public CloudFormationStackViewModel AddStack(string stackName)
        {
            var model = new CloudFormationStackViewModel(this._metaNode.CloudFormationStackViewMetaNode, this, stackName);
            base.AddChild(model);
            return model;
        }

        protected override string IconName => "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.service-root-node.png";

        public IAmazonCloudFormation CloudFormationClient => this._cloudFormationClient;

        protected override void BuildClient(AWSCredentials awsCredentials)
        {
            var config = new AmazonCloudFormationConfig {MaxErrorRetry = 6};
            this.CurrentEndPoint.ApplyToClientConfig(config);
            this._cloudFormationClient = new AmazonCloudFormationClient(awsCredentials, config);
        }


        protected override void LoadChildren()
        {
            var items = new List<IViewModel>();
            var response = new DescribeStacksResponse();
            do
            {
                var request = new DescribeStacksRequest() { NextToken = response.NextToken };
                ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

                response = this.CloudFormationClient.DescribeStacks(request);

                foreach (var stack in response.Stacks)
                {
                    if (stack.StackStatus == CloudFormationConstants.DeleteInProgressStatus ||
                        stack.StackStatus == CloudFormationConstants.DeleteCompleteStatus)
                        continue;

                    var child = new CloudFormationStackViewModel(this._metaNode.CloudFormationStackViewMetaNode, this, stack.StackName);
                    items.Add(child);
                }
            } while (!string.IsNullOrEmpty(response.NextToken));


            BeginCopingChildren(items);
        }    
    }
}
