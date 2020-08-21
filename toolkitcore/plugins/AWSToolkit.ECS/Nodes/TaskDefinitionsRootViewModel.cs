using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;
using Amazon.ECS.Model;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class TaskDefinitionsRootViewModel : InstanceDataRootViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TaskDefinitionsRootViewModel));

        readonly RootViewModel _rootViewModel;
        readonly IAmazonECS _ecsClient;

        public TaskDefinitionsRootViewModel(TaskDefinitionsRootViewMetaNode metaNode, RootViewModel viewModel)
            : base(metaNode, viewModel, "Task Definitions")
        {
            this._rootViewModel = viewModel;
            this._ecsClient = viewModel.ECSClient;
        }

        public IAmazonECS ECSClient => this._ecsClient;

        protected override string IconName => "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.taskdef.png";

        protected override void LoadChildren()
        {
            var items = new List<IViewModel>();

            var request = new ListTaskDefinitionsRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            do
            {
                var response = this.ECSClient.ListTaskDefinitions(request);
                items.AddRange(response.TaskDefinitionArns.Select(task =>
                    new TaskDefinitionViewModel(this.MetaNode.FindChild<TaskDefinitionViewMetaNode>(),
                        this._rootViewModel,
                        new TaskDefinitionWrapper(task))).Cast<IViewModel>().ToList());

                request.NextToken = response.NextToken;
            } while (!string.IsNullOrEmpty(request.NextToken));

            SetChildren(items);
        }

        public void RemoveTaskDefinitionInstance(string taskDefinitionArn)
        {
            base.RemoveChild(taskDefinitionArn);
        }

        public void AddTaskDefinition(TaskDefinitionWrapper instance)
        {
            var child = new TaskDefinitionViewModel(this.MetaNode.FindChild<TaskDefinitionViewMetaNode>(), this._rootViewModel, instance);
            base.AddChild(child);
        }
    }
}
